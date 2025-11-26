using Framework.Buffs.Components;
using Framework.Damage.Components;
using Framework.Damage.Policies;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Damage.Runtime
{
    internal static class DamageResolverUtility
    {
        public static int Resolve(in Entity target,
                                  in DamagePacket packet,
                                  int armor,
                                  float resistPercent,
                                  bool hasSnapshot,
                                  in BuffStatSnapshot snapshot)
        {
            int raw = math.max(0, packet.Amount);
            if (raw <= 0)
                return 0;

            int effectiveArmor = packet.IgnoreArmor != 0 ? 0 : armor;
            float effectiveResist = packet.IgnoreResist != 0 ? 0f : resistPercent;

            int mitigated = (packet.IgnoreArmor != 0 && packet.IgnoreResist != 0)
                ? raw
                : DamagePolicy.Mitigate(raw, effectiveArmor, effectiveResist);

            if (hasSnapshot && packet.IgnoreSnapshotModifiers == 0)
            {
                mitigated = ApplyDefenseMultiplier(mitigated, snapshot.DefenseMultiplier);
            }

            DamageDebug.Log($"[DamageResolver] target={DamageDebug.Format(target)} in={raw} out={mitigated} armor={armor} resist={resistPercent:0.000} effArmor={effectiveArmor} effResist={effectiveResist:0.000} ignArmor={packet.IgnoreArmor} ignResist={packet.IgnoreResist} ignSnap={packet.IgnoreSnapshotModifiers}");
            return mitigated;
        }

        public static int ComputeReflection(int dealtDamage, in BuffStatSnapshot snapshot)
        {
            int reflected = snapshot.DamageReflectFlat;
            if (snapshot.DamageReflectPercent > 0f)
            {
                float pct = math.max(0f, snapshot.DamageReflectPercent);
                reflected += (int)math.round(dealtDamage * pct);
            }
            return math.max(0, reflected);
        }

        private static int ApplyDefenseMultiplier(int value, float defenseMultiplier)
        {
            if (defenseMultiplier <= 0f || math.abs(defenseMultiplier - 1f) < 0.0001f)
                return value;
            int scaled = (int)math.round(value * defenseMultiplier);
            return math.max(0, scaled);
        }
    }
}
