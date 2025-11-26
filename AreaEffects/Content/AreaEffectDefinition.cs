using Unity.Collections;
using Unity.Mathematics;

namespace Framework.AreaEffects.Content
{
    public enum AreaShape { Circle, AABB }

    public struct AreaEffectDefinition
    {
        public FixedString64Bytes Id;
        public AreaShape Shape;
        public float Radius;  // for circle
        public float2 Size;   // for AABB (x=width,y=height)
        public float Lifetime;
        public FixedString64Bytes TeamFilter; // e.g., "All", "Allies", "Enemies"
    }
}
