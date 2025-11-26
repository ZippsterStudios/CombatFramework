using Framework.Cooldowns.Factory;
using Framework.Spells.Content;
using Framework.Spells.Pipeline.Components;
using Framework.Spells.Pipeline.Config;
using Framework.Spells.Pipeline.Events;
using Framework.Spells.Pipeline.Plan;
using Framework.Spells.Requests;
using Framework.Spells.Policies;
using Framework.Temporal.Components;
using Framework.Temporal.Policies;
using Unity.Collections;
using Unity.Entities;

namespace Framework.Spells.Pipeline.Systems
{
    [UpdateInGroup(typeof(SpellPipelineSystemGroup))]
    [UpdateAfter(typeof(CastPipelineBootstrapSystem))]
    [UpdateBefore(typeof(CastPipelineRunnerSystem))]
    public partial struct CastPlanBuilderSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<CastGlobalConfigSingleton>(out var configSingleton) || !configSingleton.IsCreated)
                return;

            var beginSim = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = beginSim.CreateCommandBuffer(state.WorldUnmanaged);
            var em = state.EntityManager;
            double now = SystemAPI.Time.ElapsedTime;
            var pendingCooldowns = new NativeList<PendingCooldown>(Allocator.Temp);

            try
            {
                foreach (var (requests, owner) in SystemAPI.Query<DynamicBuffer<SpellCastRequest>>().WithEntityAccess())
                {
                    if (requests.Length == 0)
                        continue;

                    for (int i = 0; i < requests.Length; i++)
                    {
                        var request = requests[i];
                        if (!SpellDefinitionCatalog.TryGetBlob(request.SpellKey, out var defBlob))
                            continue;

                        if (SpellPolicy.ValidateCast(em, request.Caster, request.SpellKey, now) != SpellPolicy.Result.Allow)
                            continue;

                        ref var def = ref defBlob.Value;

                        var castEntity = ecb.CreateEntity();
                        var castData = new SpellCastData
                        {
                            Caster = request.Caster,
                            Target = request.Target,
                            SpellId = request.SpellKey,
                            Definition = defBlob
                        };
                        ecb.AddComponent(castEntity, castData);

                        var flags = CastContextFlags.Began;
                        if ((def.Flags & SpellDefinitionFlags.AllowPartialRefund) != 0 ||
                            configSingleton.Reference.Value.AllowPartialRefundOnPreResolve != 0)
                        {
                            flags |= CastContextFlags.PartialRefundAllowed;
                        }

                        ecb.AddComponent(castEntity, new SpellCastContext
                        {
                            StepIndex = 0,
                            CurrentStep = CastStepType.None,
                            StepTimer = 0f,
                            StepDuration = 0f,
                            Flags = flags,
                            Termination = CastTerminationReason.None
                        });

                        var plan = ecb.AddBuffer<CastPlanStep>(castEntity);
                        BuildDefaultPlan(ref plan, ref def);

                        if (em.HasBuffer<SpellCastPlanModifier>(request.Caster))
                        {
                            var modifiers = em.GetBuffer<SpellCastPlanModifier>(request.Caster);
                            ApplyPlanModifiers(ref plan, modifiers);
                        }

                        SpellCastEventUtility.Emit<SpellBeganEvent>(ecb, castEntity, castData);
                        ScheduleCooldown(ref em, in request, ref def, now, ref pendingCooldowns);
                    }

                    requests.Clear();
                }

                for (int i = 0; i < pendingCooldowns.Length; i++)
                {
                    var pending = pendingCooldowns[i];
                    if (!em.Exists(pending.Target))
                        continue;
                    CooldownFactory.ApplyCooldown(ref em, pending.Target, pending.GroupId, pending.ReadyTime);
                }
            }
            finally
            {
                pendingCooldowns.Dispose();
            }
        }

        static void BuildDefaultPlan(ref DynamicBuffer<CastPlanStep> plan, ref SpellDefinitionBlob def)
        {
            plan.Add(new CastPlanStep { StepType = CastStepType.Validate, Params = CastStepParams.Empty });
            plan.Add(new CastPlanStep { StepType = CastStepType.Afford, Params = CastStepParams.Empty });
            plan.Add(new CastPlanStep { StepType = CastStepType.Spend, Params = CastStepParams.Empty });

            if (def.CastTime > 0f || (def.Flags & SpellDefinitionFlags.Channeled) != 0)
            {
                plan.Add(new CastPlanStep
                {
                    StepType = CastStepType.Windup,
                    Params = new CastStepParams { Float0 = def.CastTime }
                });
            }

            plan.Add(new CastPlanStep { StepType = CastStepType.InterruptCheck, Params = CastStepParams.Empty });
            plan.Add(new CastPlanStep { StepType = CastStepType.FizzleCheck, Params = CastStepParams.Empty });
            plan.Add(new CastPlanStep { StepType = CastStepType.Apply, Params = CastStepParams.Empty });
            plan.Add(new CastPlanStep { StepType = CastStepType.Cleanup, Params = CastStepParams.Empty });
        }

        static void ApplyPlanModifiers(ref DynamicBuffer<CastPlanStep> plan, DynamicBuffer<SpellCastPlanModifier> modifiers)
        {
            for (int i = 0; i < modifiers.Length; i++)
            {
                var mod = modifiers[i];
                switch (mod.Operation)
                {
                    case CastPlanOperation.Append:
                        plan.Add(new CastPlanStep { StepType = mod.StepType, Params = mod.Params });
                        break;
                    case CastPlanOperation.Remove:
                        RemoveFirst(ref plan, mod.StepType);
                        break;
                    case CastPlanOperation.InsertBefore:
                        InsertRelative(ref plan, mod, true);
                        break;
                    case CastPlanOperation.InsertAfter:
                        InsertRelative(ref plan, mod, false);
                        break;
                }
            }
        }

        static void RemoveFirst(ref DynamicBuffer<CastPlanStep> plan, CastStepType type)
        {
            for (int i = 0; i < plan.Length; i++)
            {
                if (plan[i].StepType == type)
                {
                    plan.RemoveAt(i);
                    return;
                }
            }
        }

        static void InsertRelative(ref DynamicBuffer<CastPlanStep> plan, in SpellCastPlanModifier mod, bool before)
        {
            for (int i = 0; i < plan.Length; i++)
            {
                if (plan[i].StepType == mod.ReferenceStep)
                {
                    var insertIndex = before ? i : i + 1;
                    if (insertIndex >= plan.Length)
                    {
                        plan.Add(new CastPlanStep { StepType = mod.StepType, Params = mod.Params });
                    }
                    else
                    {
                        plan.Insert(insertIndex, new CastPlanStep { StepType = mod.StepType, Params = mod.Params });
                    }
                    return;
                }
            }
        }

        static void ScheduleCooldown(ref EntityManager em, in SpellCastRequest request, ref SpellDefinitionBlob def, double now, ref NativeList<PendingCooldown> pendingCooldowns)
        {
            if (def.Cooldown <= 0f)
                return;

            float temporalMul = 1f;
            if (em.HasComponent<TemporalModifiers>(request.Caster))
            {
                var modifiers = em.GetComponentData<TemporalModifiers>(request.Caster);
                temporalMul = TemporalPolicy.IntervalMultiplier(modifiers.HastePercent, modifiers.SlowPercent);
                if (temporalMul <= 0f)
                    temporalMul = 1f;
            }

            var readyTime = now + def.Cooldown * temporalMul;
            pendingCooldowns.Add(new PendingCooldown
            {
                Target = request.Caster,
                GroupId = request.SpellKey,
                ReadyTime = readyTime
            });
        }

        private struct PendingCooldown
        {
            public Entity Target;
            public FixedString64Bytes GroupId;
            public double ReadyTime;
        }
    }
}
