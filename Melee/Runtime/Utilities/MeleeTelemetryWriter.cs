using Framework.Melee.Components;
using Unity.Collections;
using Unity.Entities;

namespace Framework.Melee.Runtime.Utilities
{
    public static class MeleeTelemetryWriter
    {
        public static void Write(ref DynamicBuffer<MeleeTelemetryEvent> buffer,
                                 MeleeTelemetryEventType type,
                                 in Entity attacker,
                                 in Entity target,
                                 in FixedString32Bytes weaponSlot,
                                 uint requestId,
                                 float value0 = 0f,
                                 float value1 = 0f,
                                 byte flags = 0)
        {
            buffer.Add(new MeleeTelemetryEvent
            {
                EventType = type,
                Attacker = attacker,
                Target = target,
                WeaponSlot = weaponSlot.ConvertToFixedString64(),
                RequestId = requestId,
                Value0 = value0,
                Value1 = value1,
                Flags = flags
            });
        }

        private static FixedString64Bytes ConvertToFixedString64(this FixedString32Bytes value)
        {
            FixedString64Bytes converted = default;
            converted.Append(value);
            return converted;
        }
    }
}
