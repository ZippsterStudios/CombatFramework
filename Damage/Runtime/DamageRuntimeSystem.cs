using Framework.Buffs.Components;
using Framework.Core.Telemetry;
using Framework.Damage.Components;
using Framework.Damage.Requests;
using Framework.Resources.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Damage.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(Framework.Core.Base.RuntimeSystemGroup), OrderFirst = true)]
    public partial struct DamageRuntimeSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            em.CompleteDependencyBeforeRW<DamageRequest>();
            em.CompleteDependencyBeforeRW<Health>();

            var damageables = SystemAPI.GetComponentLookup<Damageable>(isReadOnly: true);
            damageables.Update(ref state);

            var buffSnapshots = SystemAPI.GetComponentLookup<BuffStatSnapshot>(isReadOnly: true);
            buffSnapshots.Update(ref state);

            bool telemetryEnabled = TelemetryRouter.IsEnabled();

            foreach (var (requests, healthRef, entity) in SystemAPI.Query<DynamicBuffer<DamageRequest>, RefRW<Health>>().WithEntityAccess())
            {
                ref var health = ref healthRef.ValueRW;

                int armor = 0;
                float resist = 0f;
                if (damageables.HasComponent(entity))
                {
                    var dmg = damageables[entity];
                    armor = dmg.Armor;
                    resist = dmg.ResistPercent;
                }

                bool hasSnapshot = buffSnapshots.HasComponent(entity);
                var snapshot = hasSnapshot ? buffSnapshots[entity] : default;

                for (int i = 0; i < requests.Length; i++)
                {
                    var packet = requests[i].Packet;
                    int mitigated = DamageResolverUtility.Resolve(entity, packet, armor, resist, hasSnapshot, snapshot);
                    if (mitigated <= 0)
                        continue;

                    long next = (long)health.Current - mitigated;
                    if (next < 0) next = 0;
                    health.Current = (int)next;

                    if (telemetryEnabled)
                        EmitDamageTelemetry(mitigated);

                    if (packet.IgnoreSnapshotModifiers != 0 || !hasSnapshot)
                        continue;

                    int reflected = DamageResolverUtility.ComputeReflection(mitigated, snapshot);
                    if (reflected <= 0)
                        continue;

                    var attacker = packet.Source;
                    if (attacker == Entity.Null || !em.Exists(attacker))
                        continue;

                    if (!em.HasBuffer<DamageRequest>(attacker))
                        em.AddBuffer<DamageRequest>(attacker);
                    var attackerBuffer = em.GetBuffer<DamageRequest>(attacker);
                    attackerBuffer.Add(new DamageRequest
                    {
                        Target = attacker,
                        Packet = new DamagePacket
                        {
                            Amount = reflected,
                            Source = entity,
                            School = packet.School,
                            Tags = packet.Tags,
                            CritMult = 1f
                        }
                    });

                    if (telemetryEnabled)
                        EmitReflectTelemetry(reflected);
                }

                requests.Clear();
            }
        }

        [BurstDiscard]
        private static void EmitDamageTelemetry(int amount)
        {
            var tag = new FixedString64Bytes("DamageApplied");
            TelemetryRouter.Emit(tag, amount);
        }

        [BurstDiscard]
        private static void EmitReflectTelemetry(int amount)
        {
            var tag = new FixedString64Bytes("DamageReflected");
            TelemetryRouter.Emit(tag, amount);
        }
    }
}
