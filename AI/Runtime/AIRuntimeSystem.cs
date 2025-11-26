using Framework.AI.Components;
using Framework.Contracts.Intents;
using Unity.Burst;
using Unity.Entities;

namespace Framework.AI.Runtime
{
    [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low, CompileSynchronously = true)]
    [UpdateInGroup(typeof(Framework.Core.Base.RequestsSystemGroup))]
    public partial struct AIRuntimeSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }
        [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low, CompileSynchronously = true)]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (aiState, buffer, entity) in
                     SystemAPI.Query<RefRW<AIState>, DynamicBuffer<StateChangeRequest>>()
                         .WithAll<AIBehaviorEnabledTag>()
                         .WithEntityAccess())
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    var req = buffer[i];
                    if (Policies.AIPolicy.ValidateState(req.DesiredState) == Policies.AIPolicy.Result.Allow)
                        aiState.ValueRW.Current = req.DesiredState;
                }
                buffer.Clear();
            }
        }
    }
}
