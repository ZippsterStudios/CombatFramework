using Unity.Burst;
using Unity.Entities;
using Framework.Damage.Components;
using Framework.Spells.TemporalImprint.Components;

namespace Framework.Spells.TemporalImprint.Systems
{
    /// <summary>
    /// Replays recorded timeline events from temporal echoes back into the runtime systems.
    /// This is a minimal dispatcher that enqueues damage/heal/buff/debuff/DOT/HOT using existing factories.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(Framework.Core.Base.RuntimeSystemGroup))]
    public partial struct TemporalImprintReplaySystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            double now = state.WorldUnmanaged.Time.ElapsedTime;
            var em = state.EntityManager;

            foreach (var (echo, timeline, entity) in SystemAPI.Query<RefRW<TemporalEcho>, DynamicBuffer<TimelineEvent>>().WithEntityAccess())
            {
                ref var echoData = ref echo.ValueRW;

                bool expired = now - echoData.StartTime >= echoData.ReplayDuration;
                bool dead = em.HasComponent<Components.TemporalEchoHealth>(entity) && em.GetComponentData<Components.TemporalEchoHealth>(entity).Current <= 0f;

                if (expired || dead)
                {
                    if (dead)
                        InvertRemaining(ref em, in timeline, echoData.Cursor);
                    em.DestroyEntity(entity);
                    continue;
                }

                while (echoData.Cursor < timeline.Length)
                {
                    var evt = timeline[echoData.Cursor];
                    if (evt.State != TimelineEventState.Pending)
                    {
                        echoData.Cursor++;
                        continue;
                    }

                    // Execute when the replay time reaches the event time.
                    if ((float)(now - echoData.StartTime) < evt.Time)
                        break;

                    DispatchEvent(ref em, in evt);
                    evt.State = TimelineEventState.Processed;
                    timeline[echoData.Cursor] = evt;
                    echoData.Cursor++;
                }
            }
        }

        private static TimelineEvent Invert(in TimelineEvent evt)
        {
            var inverted = evt;
            switch (evt.Kind)
            {
                case TimelineEventKind.Damage:
                    inverted.Kind = TimelineEventKind.Heal;
                    break;
                case TimelineEventKind.Heal:
                    inverted.Kind = TimelineEventKind.Damage;
                    break;
                case TimelineEventKind.ApplyBuff:
                    inverted.Kind = TimelineEventKind.ApplyDebuff;
                    break;
                case TimelineEventKind.ApplyDebuff:
                    inverted.Kind = TimelineEventKind.ApplyBuff;
                    break;
                case TimelineEventKind.Dot:
                    inverted.Kind = TimelineEventKind.Hot;
                    break;
                case TimelineEventKind.Hot:
                    inverted.Kind = TimelineEventKind.Dot;
                    break;
            }
            inverted.State = TimelineEventState.Pending;
            return inverted;
        }

        private static void InvertRemaining(ref EntityManager em, in DynamicBuffer<TimelineEvent> timeline, int cursor)
        {
            for (int i = cursor; i < timeline.Length; i++)
            {
                var evt = timeline[i];
                if (evt.State == TimelineEventState.Processed)
                    continue;
                var inverted = Invert(in evt);
                DispatchEvent(ref em, in inverted, default);
            }
        }

        private static void DispatchEvent(ref EntityManager em, in TimelineEvent evt, in TemporalEcho echo)
        {
            float dmgMul = echo.DamageMultiplier;
            float healMul = echo.HealMultiplier;
            float hitboxScale = echo.HitboxScale > 0f ? echo.HitboxScale : 1f;
            bool autoAim = echo.AutoAim != 0 || evt.AutoAim != 0;

            switch (evt.Kind)
            {
                case TimelineEventKind.Damage:
                    Framework.Damage.Factory.DamageFactory.EnqueueDamage(ref em, evt.Target, new DamagePacket
                    {
                        Amount = (int)(evt.Amount * dmgMul),
                        School = evt.School,
                        Source = evt.Caster,
                        CritMult = 1f
                    });
                    break;
                case TimelineEventKind.Heal:
                    Framework.Heal.Factory.HealFactory.EnqueueHeal(ref em, evt.Target, (int)(evt.Amount * healMul));
                    break;
                case TimelineEventKind.ApplyBuff:
                    Framework.Buffs.Factory.BuffFactory.Apply(ref em, evt.Target, evt.EffectId, evt.Duration, 1);
                    break;
                case TimelineEventKind.ApplyDebuff:
                    Framework.Debuffs.Factory.DebuffFactory.Enqueue(ref em, evt.Target, evt.EffectId, evt.Duration, 1, evt.Caster);
                    break;
                case TimelineEventKind.Dot:
                    Framework.DOT.Factory.DotFactory.Enqueue(ref em, evt.Target, evt.EffectId, evt.Amount, evt.TickInterval, evt.Duration, evt.Caster, default, 0, 0);
                    break;
                case TimelineEventKind.Hot:
                    Framework.HOT.Factory.HotFactory.Enqueue(ref em, evt.Target, evt.EffectId, evt.Amount, evt.TickInterval, evt.Duration, evt.Caster, default, 0, 0);
                    break;
                case TimelineEventKind.SummonPet:
                    Framework.Spells.Features.PetSummonBridge.TrySummon(ref em, evt.Caster, evt.Target, default, new Framework.Spells.Content.SummonPayload
                    {
                        PetId = evt.EffectId,
                        Count = evt.Amount,
                        SpawnRadius = evt.Radius * hitboxScale
                    }, 0);
                    break;
                case TimelineEventKind.Script:
                    Framework.Spells.Runtime.SpellScriptBridge.TryInvoke(ref em, evt.Caster, evt.Target, default, evt.EffectId, default);
                    break;
            }
        }
    }
}
