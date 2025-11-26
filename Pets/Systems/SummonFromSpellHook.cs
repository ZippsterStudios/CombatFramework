using Framework.Core.Base;
using Framework.Pets.Factory;
using Framework.Spells.Features;
using Unity.Burst;
using Unity.Entities;

namespace Framework.Pets.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(RequestsSystemGroup))]
    public partial struct SummonFromSpellHook : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            PetSummonBridge.Register(PetFactory.HandleSummonEffect);
        }

        public void OnDestroy(ref SystemState state) { }
        public void OnUpdate(ref SystemState state) { }
    }
}
