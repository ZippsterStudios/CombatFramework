using Framework.Debuffs.Components;
using Framework.Debuffs.Content;
using Framework.TimedEffect.Components;
using Framework.TimedEffect.Content;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Debuffs.Resolution
{
    [BurstCompile]
    [UpdateInGroup(typeof(Framework.Core.Base.ResolutionSystemGroup))]
    public partial struct DebuffResolutionSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.EntityManager.CompleteDependencyBeforeRW<TimedEffectInstance>();
            state.EntityManager.CompleteDependencyBeforeRW<DebuffInstance>();

            var em = state.EntityManager;
            var query = SystemAPI.QueryBuilder()
                                 .WithAllRW<TimedEffectInstance, DebuffInstance>()
                                 .Build();
            using var entities = query.ToEntityArray(Allocator.Temp);

            for (int eIndex = 0; eIndex < entities.Length; eIndex++)
            {
                var entity = entities[eIndex];
                var timed = em.GetBuffer<TimedEffectInstance>(entity);
                var debuffs = em.GetBuffer<DebuffInstance>(entity);

                var statList = new NativeList<DebuffStatAggregate>(Allocator.Temp);
                var activeIds = new NativeHashSet<FixedString64Bytes>(math.max(1, timed.Length), Allocator.Temp);
                try
                {
                    DebuffFlags aggregatedFlags = DebuffFlags.None;
                    bool hasDebuff = false;

                    for (int i = 0; i < timed.Length; i++)
                    {
                        var inst = timed[i];
                        if (inst.Type != TimedEffectType.Debuff)
                            continue;

                        int index = IndexOf(ref debuffs, inst.EffectId);
                        if (index < 0)
                            continue;

                        hasDebuff = true;
                        activeIds.Add(inst.EffectId);

                        var payload = debuffs[index];
                        aggregatedFlags |= payload.Flags;

                        int stacks = math.max(1, inst.StackCount);
                        AccumulateStatEffects(ref statList, payload.StatEffects, stacks);
                    }

                    if (!hasDebuff)
                    {
                        if (debuffs.Length > 0)
                            debuffs.Clear();
                        RemoveDebuffState(ref em, entity);
                        continue;
                    }

                    // prune any cached DebuffInstance entries that no longer have an active timed effect
                    for (int i = debuffs.Length - 1; i >= 0; i--)
                    {
                        if (!activeIds.Contains(debuffs[i].DebuffId))
                            debuffs.RemoveAt(i);
                    }

                    ApplyCrowdControlState(ref em, entity, aggregatedFlags);
                    ApplyStatAggregates(ref em, entity, in statList);
                }
                finally
                {
                    activeIds.Dispose();
                    statList.Dispose();
                }
            }
        }

        private static void AccumulateStatEffects(ref NativeList<DebuffStatAggregate> list, in FixedList128Bytes<DebuffStatEffect> effects, int stacks)
        {
            for (int i = 0; i < effects.Length; i++)
            {
                var eff = effects[i];
                float additive = eff.AdditivePerStack * stacks;
                float multiplier = math.pow(eff.MultiplierPerStack <= 0f ? 1f : eff.MultiplierPerStack, math.max(1, stacks));

                bool found = false;
                for (int j = 0; j < list.Length; j++)
                {
                    if (list[j].StatId.Equals(eff.StatId))
                    {
                        var agg = list[j];
                        agg.Additive += additive;
                        agg.Multiplier *= multiplier;
                        list[j] = agg;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    list.Add(new DebuffStatAggregate
                    {
                        StatId = eff.StatId,
                        Additive = additive,
                        Multiplier = multiplier
                    });
                }
            }
        }

        private static int IndexOf(ref DynamicBuffer<DebuffInstance> buffer, in FixedString64Bytes id)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].DebuffId.Equals(id))
                    return i;
            }
            return -1;
        }

        private static void RemoveDebuffState(ref EntityManager em, in Entity entity)
        {
            if (em.HasComponent<DebuffCrowdControlState>(entity))
                em.RemoveComponent<DebuffCrowdControlState>(entity);
            if (em.HasBuffer<DebuffStatAggregate>(entity))
                em.RemoveComponent<DebuffStatAggregate>(entity);
        }

        private static void ApplyCrowdControlState(ref EntityManager em, in Entity entity, DebuffFlags flags)
        {
            if (flags == DebuffFlags.None)
            {
                if (em.HasComponent<DebuffCrowdControlState>(entity))
                    em.RemoveComponent<DebuffCrowdControlState>(entity);
                return;
            }

            if (!em.HasComponent<DebuffCrowdControlState>(entity))
            {
                em.AddComponentData(entity, new DebuffCrowdControlState { ActiveFlags = flags });
            }
            else
            {
                var cc = em.GetComponentData<DebuffCrowdControlState>(entity);
                cc.ActiveFlags = flags;
                em.SetComponentData(entity, cc);
            }
        }

        private static void ApplyStatAggregates(ref EntityManager em, in Entity entity, in NativeList<DebuffStatAggregate> stats)
        {
            if (stats.Length == 0)
            {
                if (em.HasBuffer<DebuffStatAggregate>(entity))
                    em.RemoveComponent<DebuffStatAggregate>(entity);
                return;
            }

            var statBuffer = em.HasBuffer<DebuffStatAggregate>(entity)
                ? em.GetBuffer<DebuffStatAggregate>(entity)
                : em.AddBuffer<DebuffStatAggregate>(entity);
            statBuffer.Clear();
            for (int i = 0; i < stats.Length; i++)
                statBuffer.Add(stats[i]);
        }
    }
}
