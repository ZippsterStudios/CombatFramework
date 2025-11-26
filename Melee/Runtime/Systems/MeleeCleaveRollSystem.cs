using Framework.Melee.Components;
using Framework.Melee.Runtime.SystemGroups;
using Framework.Melee.Runtime.Utilities;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Melee.Runtime.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(MeleeSystemGroup))]
    [UpdateAfter(typeof(MeleePhaseSystem))]
    [UpdateBefore(typeof(MeleeHitDetectionSystem))]
    public partial struct MeleeCleaveRollSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var telemetry = SystemAPI.GetSingletonBuffer<MeleeTelemetryEvent>();
            var em = state.EntityManager;

            foreach (var (contextRef, entity) in SystemAPI.Query<RefRW<MeleeCastContext>>().WithEntityAccess())
            {
                ref var context = ref contextRef.ValueRW;
                if (context.CleaveResolved != 0 || context.Phase != MeleePhaseState.Active || !context.Definition.IsCreated)
                    continue;

                var stats = em.HasComponent<MeleeStatSnapshot>(context.Attacker)
                    ? em.GetComponentData<MeleeStatSnapshot>(context.Attacker)
                    : default;

                ref var def = ref context.Definition.Value;
                float chance = math.max(0f, stats.FrontalArcChance);
                if (chance <= 0f)
                {
                    context.CleaveMode = false;
                    context.CleaveArcDegrees = def.DefaultCleaveArcDegrees;
                    context.CleaveMaxTargets = math.max(1, def.DefaultCleaveMaxTargets);
                    context.CleaveResolved = 1;
                    continue;
                }

                var rng = MeleeDeterministicRng.FromRaw(context.DeterministicSeed);
                bool cleave = rng.RollPercent(chance);
                context.DeterministicSeed = rng.SerializeState();
                context.CleaveResolved = 1;

                if (cleave)
                {
                    context.CleaveMode = true;
                    context.CleaveArcDegrees = stats.FrontalArcDegrees > 0f ? stats.FrontalArcDegrees : def.DefaultCleaveArcDegrees;
                    context.CleaveMaxTargets = stats.FrontalArcMaxTargets > 0 ? stats.FrontalArcMaxTargets : def.DefaultCleaveMaxTargets;
                    if (stats.FrontalArcPenetration > 0)
                        context.PenetrationRemaining = stats.FrontalArcPenetration;

                    MeleeTelemetryWriter.Write(ref telemetry, MeleeTelemetryEventType.CleaveTriggered, context.Attacker, Entity.Null, context.WeaponSlot, context.SequenceId, context.CleaveArcDegrees, context.CleaveMaxTargets);
                }
                else
                {
                    context.CleaveMode = false;
                    context.CleaveArcDegrees = def.DefaultCleaveArcDegrees;
                    context.CleaveMaxTargets = math.max(1, def.DefaultCleaveMaxTargets);
                }
            }
        }
    }
}
