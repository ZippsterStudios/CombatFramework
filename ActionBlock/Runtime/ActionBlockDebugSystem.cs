#if UNITY_EDITOR
using Framework.ActionBlock.Components;
using Unity.Entities;
using UnityEngine;

namespace Framework.ActionBlock.Runtime
{
    [UpdateInGroup(typeof(Framework.Core.Base.TelemetrySystemGroup))]
    public partial struct ActionBlockDebugSystem : ISystem
    {
        private double _nextLog;

        public void OnCreate(ref SystemState state)
        {
            _nextLog = 0d;
        }

        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state)
        {
            double now = SystemAPI.Time.ElapsedTime;
            if (now < _nextLog)
                return;

            _nextLog = now + 1d;

            foreach (var (mask, entity) in SystemAPI.Query<RefRO<ActionBlockMask>>().WithEntityAccess())
            {
                if (mask.ValueRO.Bits == 0)
                    continue;
                Debug.Log($"[ActionBlock] Entity {entity.Index}:{entity.Version} -> 0x{mask.ValueRO.Bits:X}");
            }
        }
    }
}
#endif
