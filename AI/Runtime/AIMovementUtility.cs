using Framework.Contracts.Intents;
using Unity.Mathematics;

namespace Framework.AI.Runtime
{
    public static class AIMovementUtility
    {
        public static void ClampToLeash(byte hasLeash, in float2 home, float radius, float softRadius, ref MoveIntent intent)
        {
            if (intent.Active == 0 || hasLeash == 0)
                return;

            var delta = intent.Destination - home;
            var limit = math.max(0.1f, radius);
            var distSq = delta.x * delta.x + delta.y * delta.y;

            if (distSq <= limit * limit)
                return;

            var soft = math.max(limit, softRadius);
            var dir = NormalizeSafe(delta);
            intent.Destination = home + dir * soft;
        }

        private static float2 NormalizeSafe(in float2 value)
        {
            var lenSq = value.x * value.x + value.y * value.y;
            if (lenSq <= 1e-5f)
                return new float2(1f, 0f);
            return value / math.sqrt(lenSq);
        }
    }
}
