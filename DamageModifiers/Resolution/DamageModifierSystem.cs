using Framework.Damage.Components;
using Framework.Damage.Requests;
using Framework.DamageModifiers.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace Framework.DamageModifiers.Resolution
{
    [BurstCompile]
    [UpdateInGroup(typeof(Framework.Core.Base.ResolutionSystemGroup))]
    [UpdateBefore(typeof(Framework.Damage.Resolution.DamageResolutionSystem))]
    public partial struct DamageModifierSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
em.CompleteDependencyBeforeRW<DamageRequest>();
            var query = SystemAPI.QueryBuilder()
                                 .WithAllRW<DamageRequest>()
                                 .Build();
            using var entities = query.ToEntityArray(Allocator.Temp);

            for (int entityIndex = 0; entityIndex < entities.Length; entityIndex++)
            {
                var entity = entities[entityIndex];
                var requests = em.GetBuffer<DamageRequest>(entity);

                for (int i = 0; i < requests.Length; i++)
                {
                    var request = requests[i];
                    if (!em.Exists(request.Target))
                        continue;

                    int baseAmount = request.Packet.Amount;
                    if (baseAmount == 0)
                        continue;

                    float multiplier = 1f;
                    if (em.HasComponent<DamageModifierDefaults>(request.Target))
                    {
                        var defaults = em.GetComponentData<DamageModifierDefaults>(request.Target);
                        multiplier *= defaults.GlobalMultiplier;
                    }

                    if (em.HasBuffer<DamageTypeModifier>(request.Target))
                    {
                        var mods = em.GetBuffer<DamageTypeModifier>(request.Target);
                        for (int m = 0; m < mods.Length; m++)
                        {
                            var entry = mods[m];
                            if (entry.School == request.Packet.School)
                                multiplier *= entry.Multiplier;
                        }
                    }

                    if (math.abs(multiplier - 1f) <= 0.0001f)
                        continue;

                    float adjusted = baseAmount * multiplier;
                    if (adjusted > 0f)
                    {
                        request.Packet.Amount = (int)math.round(adjusted);
                        requests[i] = request;
                        continue;
                    }

                    request.Packet.Amount = 0;
                    requests[i] = request;

                    int healAmount = (int)math.round(math.abs(adjusted));
                    if (healAmount > 0)
                        Framework.Heal.Factory.HealFactory.EnqueueHeal(ref em, request.Target, healAmount);
                }
            }
        }
    }
}

