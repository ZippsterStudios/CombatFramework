using Framework.Core.Base;
using Framework.Core.Telemetry;
using Framework.Heal.Requests;
using Framework.Heal.Policies;
using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Framework.Resources.Components;

namespace Framework.Heal.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(Framework.Core.Base.RequestsSystemGroup))]
    public partial struct HealRuntimeSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }
        public void OnUpdate(ref SystemState state)
        {
            var job = new HealRuntimeJob
            {
                TelemetryEnabled = TelemetryRouter.IsEnabled()
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct HealRuntimeJob : IJobEntity
    {
        public bool TelemetryEnabled;

        void Execute(Entity entity, DynamicBuffer<HealRequest> requests, ref Health health)
        {
            for (int i = 0; i < requests.Length; i++)
            {
                int amount = requests[i].Amount;
                var policy = HealPolicy.Validate(amount);
                if (policy == HealPolicy.Result.Reject_Negative || amount <= 0)
                    continue;

                long next = (long)health.Current + amount;
                if (next > health.Max) next = health.Max;
                health.Current = (int)next;

                if (TelemetryEnabled)
                    EmitHealTelemetry(amount);
            }

            requests.Clear();
        }

        [BurstDiscard]
        static void EmitHealTelemetry(int amount)
        {
            var tag = new FixedString64Bytes("HealApplied");
            TelemetryRouter.Emit(tag, amount);
        }
    }
}
