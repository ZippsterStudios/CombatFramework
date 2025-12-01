using Framework.Core.Base;
using Framework.Spells.Content;
using Framework.Spells.Pipeline.Systems;
using Framework.Spells.Sustain;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Framework.Spells.Sustain
{
    [BurstCompile]
    [UpdateInGroup(typeof(RuntimeSystemGroup))]
    public partial struct SustainedSpellDrainSystem : ISystem
    {
        private static readonly FixedString64Bytes DefaultResource = CreateResourceLiteral();

        [BurstCompile]
        public void OnCreate(ref SystemState state) { }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            double now = SystemAPI.Time.ElapsedTime;

            foreach (var (drainsRef, entity) in SystemAPI.Query<DynamicBuffer<SustainedSpellDrain>>().WithEntityAccess())
            {
                var drains = drainsRef;
                for (int i = drains.Length - 1; i >= 0; i--)
                {
                    var drain = drains[i];

                    bool keep = SustainedDrainUtility.TryProcess(
                        ref drain,
                        now,
                        DefaultResource,
                        cost => ResourceAccessUtility.CanAfford(ref em, entity, cost),
                        cost => ResourceAccessUtility.Spend(ref em, entity, cost));

                    if (!keep)
                    {
                        drains.RemoveAt(i);
                        continue;
                    }

                    drains[i] = drain;
                }
            }
        }

        private static FixedString64Bytes CreateResourceLiteral()
        {
            FixedString64Bytes value = default;
            value.Append("Mana");
            return value;
        }
    }
}
