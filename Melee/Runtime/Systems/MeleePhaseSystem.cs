using Framework.Melee.Components;
using Framework.Melee.Runtime.SystemGroups;
using Unity.Burst;
using Unity.Entities;

namespace Framework.Melee.Runtime.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(MeleeSystemGroup))]
    [UpdateAfter(typeof(MeleePlanBuilderSystem))]
    public partial struct MeleePhaseSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;
            foreach (var contextRef in SystemAPI.Query<RefRW<MeleeCastContext>>())
            {
                ref var context = ref contextRef.ValueRW;
                if (context.Completed != 0)
                    continue;

                context.PhaseTimer += dt;
                switch (context.Phase)
                {
                    case MeleePhaseState.Windup:
                        if (context.PhaseTimer >= context.WindupTime)
                        {
                            context.Phase = MeleePhaseState.Active;
                            context.PhaseTimer = 0f;
                        }
                        break;

                    case MeleePhaseState.Active:
                        if (context.PhaseTimer >= context.ActiveTime)
                        {
                            context.Phase = MeleePhaseState.Recovery;
                            context.PhaseTimer = 0f;
                        }
                        break;

                    case MeleePhaseState.Recovery:
                        if (context.PhaseTimer >= context.RecoveryTime)
                        {
                            context.Completed = 1;
                        }
                        break;
                }
            }
        }
    }
}
