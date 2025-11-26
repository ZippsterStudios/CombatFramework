using Unity.Entities;

namespace Framework.Cooldowns.Requests
{
    public struct CooldownRequest : IBufferElementData
    {
        public Entity Target;
        public double Cooldown;
        public double Now;
    }
}
