using Unity.Mathematics;

namespace Framework.Melee.Runtime.Utilities
{
    public static class MeleeArcUtility
    {
        public static bool IsWithinArc(float3 forward, float3 toTarget, float arcDegrees)
        {
            if (math.lengthsq(toTarget) <= 1e-6f)
                return true;

            var normalizedForward = math.normalize(forward);
            if (math.lengthsq(normalizedForward) <= 1e-6f)
                normalizedForward = new float3(0f, 0f, 1f);

            var dir = math.normalize(toTarget);
            float halfRadians = math.radians(math.clamp(arcDegrees * 0.5f, 0f, 180f));
            float dot = math.clamp(math.dot(normalizedForward, dir), -1f, 1f);
            float angle = math.acos(dot);
            return angle <= halfRadians + 1e-5f;
        }
    }
}
