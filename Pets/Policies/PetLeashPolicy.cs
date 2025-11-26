using Framework.Contracts.Perception;
using Framework.Pets.Components;
using Framework.Pets.Content;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Pets.Policies
{
    public static class PetLeashPolicy
    {
        public static void ApplyDefault(ref EntityManager em, in Entity pet, in PetDefinition def, in float2 home)
        {
            float radius = def.DefaultLeashDistance <= 0f ? 20f : def.DefaultLeashDistance;
            float soft = math.max(radius * 1.2f, radius);

            var leash = new LeashConfig
            {
                Home = home,
                Radius = radius,
                SoftRadius = soft
            };

            if (em.HasComponent<LeashConfig>(pet))
                em.SetComponentData(pet, leash);
            else
                em.AddComponentData(pet, leash);

            var shim = new PetLeashConfigShim
            {
                Home = home,
                Radius = radius,
                SoftRadius = soft,
                TeleportOnBreach = (byte)((def.Flags & PetFlags.LeashTeleport) != 0 ? 1 : 0)
            };

            if (em.HasComponent<PetLeashConfigShim>(pet))
                em.SetComponentData(pet, shim);
            else
                em.AddComponentData(pet, shim);
        }
    }
}
