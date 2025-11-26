using Unity.Mathematics;

namespace Framework.AreaEffects.Spatial.Utilities
{
    public static class Overlap
    {
        public static bool CircleContains(float2 center, float radius, float2 point)
        {
            var d = point - center;
            return math.lengthsq(d) <= radius * radius;
        }

        public static bool AABBContains(float2 min, float2 max, float2 point)
        {
            return point.x >= min.x && point.x <= max.x && point.y >= min.y && point.y <= max.y;
        }
    }
}

