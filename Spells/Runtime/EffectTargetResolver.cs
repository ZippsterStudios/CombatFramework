using Framework.Core.Components;
using Framework.Resources.Components;
using Framework.Spells.Content;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Spells.Runtime
{
    internal static class EffectTargetResolver
    {
        internal struct Context
        {
            public EntityManager EntityManager;
            public Entity Caster;
            public Entity PrimaryTarget;
        }

        public static void Resolve(ref Context ctx, in TargetScope scope, ref NativeList<Entity> results)
        {
            switch (scope.Kind)
            {
                case TargetScopeKind.Caster:
                    TryAdd(ref ctx, scope, ctx.Caster, ref results);
                    break;
                case TargetScopeKind.PrimaryTarget:
                    var target = ctx.PrimaryTarget != Entity.Null ? ctx.PrimaryTarget : (scope.IncludeCasterIfNoPrimary != 0 ? ctx.Caster : Entity.Null);
                    TryAdd(ref ctx, scope, target, ref results);
                    break;
                case TargetScopeKind.AlliesInGroupOfCaster:
                    ResolveGroup(ref ctx, scope, ref results);
                    break;
                case TargetScopeKind.AlliesInRaidOfCaster:
                    ResolveRaid(ref ctx, scope, ref results);
                    break;
                case TargetScopeKind.Radius:
                    ResolveRadius(ref ctx, scope, ref results);
                    break;
                case TargetScopeKind.ChainJump:
                    ResolveChain(ref ctx, scope, ref results);
                    break;
            }
        }

        private static void TryAdd(ref Context ctx, in TargetScope scope, in Entity candidate, ref NativeList<Entity> results)
        {
            if (candidate == Entity.Null || !ctx.EntityManager.Exists(candidate))
                return;
            if (!MatchesTeam(ref ctx, scope.TeamFilter, candidate))
                return;
            results.Add(candidate);
        }

        private static void ResolveGroup(ref Context ctx, in TargetScope scope, ref NativeList<Entity> results)
        {
            if (!ctx.EntityManager.HasComponent<GroupId>(ctx.Caster))
            {
                TryAdd(ref ctx, scope, ctx.Caster, ref results);
                return;
            }

            var casterGroup = ctx.EntityManager.GetComponentData<GroupId>(ctx.Caster).Value;
            using var entities = ctx.EntityManager.GetAllEntities(Allocator.Temp);
            foreach (var e in entities)
            {
                if (!ctx.EntityManager.HasComponent<GroupId>(e)) continue;
                if (!ctx.EntityManager.GetComponentData<GroupId>(e).Value.Equals(casterGroup)) continue;
                TryAdd(ref ctx, scope, e, ref results);
                if (scope.MaxTargets > 0 && results.Length >= scope.MaxTargets) break;
            }
        }

        private static void ResolveRaid(ref Context ctx, in TargetScope scope, ref NativeList<Entity> results)
        {
            if (!ctx.EntityManager.HasComponent<RaidId>(ctx.Caster))
            {
                TryAdd(ref ctx, scope, ctx.Caster, ref results);
                return;
            }

            var casterRaid = ctx.EntityManager.GetComponentData<RaidId>(ctx.Caster).Value;
            using var entities = ctx.EntityManager.GetAllEntities(Allocator.Temp);
            foreach (var e in entities)
            {
                if (!ctx.EntityManager.HasComponent<RaidId>(e)) continue;
                if (!ctx.EntityManager.GetComponentData<RaidId>(e).Value.Equals(casterRaid)) continue;
                TryAdd(ref ctx, scope, e, ref results);
                if (scope.MaxTargets > 0 && results.Length >= scope.MaxTargets) break;
            }
        }

        private static void ResolveRadius(ref Context ctx, in TargetScope scope, ref NativeList<Entity> results)
        {
            if (!TryGetPosition(ref ctx, scope.Center == TargetScopeCenter.PrimaryTarget ? ctx.PrimaryTarget : ctx.Caster, scope.CustomPoint, scope.Center, out var center))
                return;

            float maxDistance = scope.Radius <= 0f ? 0f : scope.Radius;
            var forward = DetermineForward(ref ctx, scope, center);

            using var entities = ctx.EntityManager.GetAllEntities(Allocator.Temp);
            foreach (var e in entities)
            {
                if (!ctx.EntityManager.HasComponent<Position>(e))
                    continue;

                var pos = ctx.EntityManager.GetComponentData<Position>(e).Value;
                var vector = pos - center;
                if (maxDistance > 0f && math.lengthsq(vector) > maxDistance * maxDistance)
                    continue;

                if (scope.Shape == TargetScopeShape.Cone && scope.ConeAngleDeg > 0f)
                {
                    var normalized = math.normalizesafe(vector, forward);
                    float dot = math.dot(normalized, forward);
                    float limit = math.cos(math.radians(scope.ConeAngleDeg * 0.5f));
                    if (dot < limit)
                        continue;
                }

                if (!MatchesTeam(ref ctx, scope.TeamFilter, e))
                    continue;

                results.Add(e);
                if (scope.MaxTargets > 0 && results.Length >= scope.MaxTargets)
                    break;
            }
        }

        private static void ResolveChain(ref Context ctx, in TargetScope scope, ref NativeList<Entity> results)
        {
            var start = ctx.PrimaryTarget != Entity.Null ? ctx.PrimaryTarget : ctx.Caster;
            if (start == Entity.Null) return;

            var visited = new NativeHashSet<Entity>(scope.Chain.MaxJumps + 1, Allocator.Temp);
            try
            {
                var current = start;
                for (int jump = 0; jump <= scope.Chain.MaxJumps && current != Entity.Null; jump++)
                {
                    if (visited.Contains(current))
                        break;
                    visited.Add(current);
                    if (MatchesTeam(ref ctx, scope.Chain.TeamFilter == TargetTeamFilter.Any ? scope.TeamFilter : scope.Chain.TeamFilter, current))
                    {
                        results.Add(current);
                    }
                    current = FindNext(ref ctx, current, scope.Chain.JumpRadius > 0f ? scope.Chain.JumpRadius : scope.Radius, scope, ref visited);
                }
            }
            finally
            {
                if (visited.IsCreated)
                    visited.Dispose();
            }
        }

        private static Entity FindNext(ref Context ctx, in Entity origin, float radius, in TargetScope scope, ref NativeHashSet<Entity> visited)
        {
            if (!ctx.EntityManager.HasComponent<Position>(origin))
                return Entity.Null;
            var originPos = ctx.EntityManager.GetComponentData<Position>(origin).Value;
            Entity best = Entity.Null;
            float bestDist = float.MaxValue;

            using var entities = ctx.EntityManager.GetAllEntities(Allocator.Temp);
            foreach (var candidate in entities)
            {
                if (candidate == origin || !ctx.EntityManager.HasComponent<Position>(candidate))
                    continue;

                if (!MatchesTeam(ref ctx, scope.Chain.TeamFilter == TargetTeamFilter.Any ? scope.TeamFilter : scope.Chain.TeamFilter, candidate))
                    continue;

                if (visited.Contains(candidate))
                    continue;

                var pos = ctx.EntityManager.GetComponentData<Position>(candidate).Value;
                float dist = math.distance(pos, originPos);
                if (radius > 0f && dist > radius)
                    continue;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = candidate;
                }
            }

            return best;
        }

        private static float2 DetermineForward(ref Context ctx, in TargetScope scope, in float2 center)
        {
            if (ctx.PrimaryTarget != Entity.Null && ctx.EntityManager.HasComponent<Position>(ctx.PrimaryTarget))
            {
                var pos = ctx.EntityManager.GetComponentData<Position>(ctx.PrimaryTarget).Value;
                var dir = pos - center;
                if (!dir.Equals(float2.zero))
                    return math.normalizesafe(dir, new float2(0f, 1f));
            }
            return new float2(0f, 1f);
        }

        private static bool MatchesTeam(ref Context ctx, TargetTeamFilter filter, in Entity candidate)
        {
            if (filter == TargetTeamFilter.Any)
                return true;

            byte casterTeam = ctx.EntityManager.HasComponent<TeamId>(ctx.Caster) ? ctx.EntityManager.GetComponentData<TeamId>(ctx.Caster).Value : (byte)0;
            byte candidateTeam = ctx.EntityManager.HasComponent<TeamId>(candidate) ? ctx.EntityManager.GetComponentData<TeamId>(candidate).Value : (byte)0;

            return filter switch
            {
                TargetTeamFilter.Ally => candidateTeam == casterTeam,
                TargetTeamFilter.Enemy => candidateTeam != casterTeam,
                _ => true
            };
        }

        private static bool TryGetPosition(ref Context ctx, in Entity entity, in float3 custom, TargetScopeCenter center, out float2 pos)
        {
            pos = float2.zero;
            if (center == TargetScopeCenter.Point)
            {
                pos = new float2(custom.x, custom.y);
                return true;
            }

            if (entity == Entity.Null || !ctx.EntityManager.HasComponent<Position>(entity))
                return false;

            pos = ctx.EntityManager.GetComponentData<Position>(entity).Value;
            return true;
        }
    }
}
