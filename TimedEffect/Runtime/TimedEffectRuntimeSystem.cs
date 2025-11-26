using Framework.Temporal.Components;
using Framework.Temporal.Policies;
using Framework.TimedEffect.Components;
using Framework.TimedEffect.Content;
using Framework.TimedEffect.Events;
using Framework.TimedEffect.Requests;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.TimedEffect.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(Framework.Core.Base.RuntimeSystemGroup))]
    public partial struct TimedEffectRuntimeSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;
            var em = state.EntityManager;

            var requestQuery = SystemAPI.QueryBuilder().WithAll<TimedEffectRequest>().Build();
            using var requestEntities = requestQuery.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < requestEntities.Length; i++)
            {
                var entity = requestEntities[i];
                if (!em.HasBuffer<TimedEffectInstance>(entity))
                    em.AddBuffer<TimedEffectInstance>(entity);
                if (!em.HasBuffer<TimedEffectEvent>(entity))
                    em.AddBuffer<TimedEffectEvent>(entity);

                var instances = em.GetBuffer<TimedEffectInstance>(entity);
                var events = em.GetBuffer<TimedEffectEvent>(entity);
                var requests = em.GetBuffer<TimedEffectRequest>(entity);

                for (int r = 0; r < requests.Length; r++)
                {
                    ProcessRequest(ref instances, requests[r], ref events, entity);
                }

                requests.Clear();
            }

            var instanceQuery = SystemAPI.QueryBuilder().WithAll<TimedEffectInstance>().Build();
            using var instanceEntities = instanceQuery.ToEntityArray(Allocator.Temp);

            for (int eIndex = 0; eIndex < instanceEntities.Length; eIndex++)
            {
                var entity = instanceEntities[eIndex];
                var instances = em.GetBuffer<TimedEffectInstance>(entity);
                if (!em.HasBuffer<TimedEffectEvent>(entity))
                    em.AddBuffer<TimedEffectEvent>(entity);
                var events = em.GetBuffer<TimedEffectEvent>(entity);
                float dtScaled = dt;
                if (em.HasComponent<TemporalModifiers>(entity))
                {
                    var tm = em.GetComponentData<TemporalModifiers>(entity);
                    var mul = TemporalPolicy.IntervalMultiplier(tm.HastePercent, tm.SlowPercent);
                    if (mul > 0f)
                        dtScaled = dt / mul;
                }

                for (int i = instances.Length - 1; i >= 0; i--)
                {
                    var inst = instances[i];

                    if (inst.Duration > 0f)
                    {
                        inst.TimeRemaining -= dtScaled;
                        if (inst.TimeRemaining < 0f)
                            inst.TimeRemaining = 0f;
                    }

                    int tickCount = 0;
                    if (inst.TickInterval > 0f)
                    {
                        inst.TimeUntilTick -= dtScaled;
                        while (inst.TimeUntilTick <= 0f)
                        {
                            inst.TimeUntilTick += inst.TickInterval;
                            tickCount++;
                        }
                    }

                    bool expired = inst.Duration > 0f && inst.TimeRemaining <= 0f;
                    if (expired)
                    {
                        events.Add(new TimedEffectEvent
                        {
                            Kind = TimedEffectEventKind.Removed,
                            EffectId = inst.EffectId,
                            Type = inst.Type,
                            CategoryId = inst.CategoryId,
                            CategoryLevel = inst.CategoryLevel,
                            StackCount = inst.StackCount,
                            Target = entity,
                            Source = inst.Source
                        });
                        instances.RemoveAt(i);
                        continue;
                    }

                    if (tickCount > 0)
                    {
                        events.Add(new TimedEffectEvent
                        {
                            Kind = TimedEffectEventKind.Tick,
                            EffectId = inst.EffectId,
                            Type = inst.Type,
                            CategoryId = inst.CategoryId,
                            CategoryLevel = inst.CategoryLevel,
                            StackCount = inst.StackCount,
                            Target = entity,
                            Source = inst.Source,
                            TickCount = tickCount
                        });
                    }

                    instances[i] = inst;
                }
            }
        }

        private static void ProcessRequest(ref DynamicBuffer<TimedEffectInstance> instances, in TimedEffectRequest request, ref DynamicBuffer<TimedEffectEvent> events, in Entity target)
        {
            if (request.EffectId.Length == 0)
                return;

            int stackableCount = request.StackableCount <= 0 ? 1 : request.StackableCount;
            int maxStacks = request.MaxStacks <= 0 ? int.MaxValue : request.MaxStacks;
            var categoryId = request.CategoryId;
            int categoryLevel = request.CategoryLevel;

            int index = IndexOf(ref instances, request.EffectId);

            if (request.StackingMode == TimedEffectStackingMode.Independent && index >= 0)
                index = -1; // treat as new instance even if existing

            if (categoryId.Length > 0 && index < 0)
            {
                RemoveLowerLevel(ref instances, categoryId, categoryLevel);
                EnsureCategoryCapacity(ref instances, categoryId, stackableCount, categoryLevel);
            }

            if (index < 0)
            {
                var inst = new TimedEffectInstance
                {
                    EffectId = request.EffectId,
                    Type = request.Type,
                    StackingMode = request.StackingMode,
                    CategoryId = categoryId,
                    CategoryLevel = categoryLevel,
                    StackableCount = stackableCount,
                    StackCount = math.max(1, request.AddStacks <= 0 ? 1 : request.AddStacks),
                    MaxStacks = maxStacks,
                    Duration = math.max(0f, request.Duration),
                    TimeRemaining = math.max(0f, request.Duration),
                    TickInterval = math.max(0f, request.TickInterval),
                    TimeUntilTick = request.TickInterval > 0f ? request.TickInterval : 0f,
                    Source = request.Source
                };
                instances.Add(inst);
                events.Add(new TimedEffectEvent
                {
                    Kind = TimedEffectEventKind.Added,
                    EffectId = inst.EffectId,
                    Type = inst.Type,
                    CategoryId = inst.CategoryId,
                    CategoryLevel = inst.CategoryLevel,
                    StackCount = inst.StackCount,
                    Target = target,
                    Source = inst.Source
                });
                return;
            }

            var existing = instances[index];
            int previousStacks = existing.StackCount;

            switch (request.StackingMode)
            {
                case TimedEffectStackingMode.RefreshDuration:
                    existing.StackCount = math.min(existing.MaxStacks, existing.StackCount + math.max(1, request.AddStacks));
                    if (request.Duration > 0f)
                    {
                        existing.Duration = request.Duration;
                        existing.TimeRemaining = request.Duration;
                    }
                    break;
                case TimedEffectStackingMode.Replace:
                    existing.StackCount = math.min(existing.MaxStacks, math.max(1, request.AddStacks));
                    if (request.Duration > 0f)
                    {
                        existing.Duration = request.Duration;
                        existing.TimeRemaining = request.Duration;
                    }
                    existing.TickInterval = math.max(0f, request.TickInterval);
                    existing.TimeUntilTick = existing.TickInterval > 0f ? existing.TickInterval : existing.TimeUntilTick;
                    existing.CategoryId = categoryId;
                    existing.CategoryLevel = categoryLevel;
                    existing.StackableCount = stackableCount;
                    break;
                case TimedEffectStackingMode.CapStacks:
                    existing.StackCount = math.min(existing.MaxStacks, existing.StackCount + math.max(1, request.AddStacks));
                    if (request.Duration > 0f)
                    {
                        existing.Duration = request.Duration;
                        existing.TimeRemaining = request.Duration;
                    }
                    break;
                case TimedEffectStackingMode.AddStacks:
                    existing.StackCount = math.min(existing.MaxStacks, existing.StackCount + math.max(1, request.AddStacks));
                    break;
                case TimedEffectStackingMode.Independent:
                    var newInst = existing;
                    newInst.StackCount = math.max(1, request.AddStacks <= 0 ? 1 : request.AddStacks);
                    newInst.Duration = math.max(0f, request.Duration);
                    newInst.TimeRemaining = math.max(0f, request.Duration);
                    newInst.TickInterval = math.max(0f, request.TickInterval);
                    newInst.TimeUntilTick = request.TickInterval > 0f ? request.TickInterval : 0f;
                    newInst.CategoryLevel = categoryLevel;
                    newInst.CategoryId = categoryId;
                    newInst.StackableCount = stackableCount;
                    instances.Add(newInst);
                    events.Add(new TimedEffectEvent
                    {
                        Kind = TimedEffectEventKind.Added,
                        EffectId = newInst.EffectId,
                        Type = newInst.Type,
                        CategoryId = newInst.CategoryId,
                        CategoryLevel = newInst.CategoryLevel,
                        StackCount = newInst.StackCount,
                        Target = target,
                        Source = newInst.Source
                    });
                    return;
            }

            instances[index] = existing;

            int stackDelta = existing.StackCount - previousStacks;
            if (stackDelta != 0)
            {
                events.Add(new TimedEffectEvent
                {
                    Kind = TimedEffectEventKind.StackChanged,
                    EffectId = existing.EffectId,
                    Type = existing.Type,
                    CategoryId = existing.CategoryId,
                    CategoryLevel = existing.CategoryLevel,
                    StackCount = existing.StackCount,
                    StackDelta = stackDelta,
                    Target = target,
                    Source = existing.Source
                });
            }
            else
            {
                events.Add(new TimedEffectEvent
                {
                    Kind = TimedEffectEventKind.Refreshed,
                    EffectId = existing.EffectId,
                    Type = existing.Type,
                    CategoryId = existing.CategoryId,
                    CategoryLevel = existing.CategoryLevel,
                    StackCount = existing.StackCount,
                    Target = target,
                    Source = existing.Source
                });
            }
        }

        private static void EnsureCategoryCapacity(ref DynamicBuffer<TimedEffectInstance> buffer, in FixedString32Bytes categoryId, int stackableCount, int categoryLevel)
        {
            if (categoryId.Length == 0)
                return;

            int allowed = stackableCount <= 0 ? 1 : stackableCount;
            var indices = new NativeList<int>(Allocator.Temp);
            var levels = new NativeList<int>(Allocator.Temp);

            for (int i = 0; i < buffer.Length; i++)
            {
                var inst = buffer[i];
                if (inst.CategoryId.Length == 0 || !inst.CategoryId.Equals(categoryId))
                    continue;
                indices.Add(i);
                levels.Add(inst.CategoryLevel);
            }

            if (indices.Length < allowed)
            {
                indices.Dispose();
                levels.Dispose();
                return;
            }

            int lowestIndex = -1;
            int lowestLevel = int.MaxValue;
            for (int i = 0; i < indices.Length; i++)
            {
                int lvl = levels[i];
                if (lvl < lowestLevel)
                {
                    lowestLevel = lvl;
                    lowestIndex = indices[i];
                }
            }

            if (lowestLevel < categoryLevel && lowestIndex >= 0)
                buffer.RemoveAt(lowestIndex);

            indices.Dispose();
            levels.Dispose();
        }

        private static void RemoveLowerLevel(ref DynamicBuffer<TimedEffectInstance> buffer, in FixedString32Bytes categoryId, int categoryLevel)
        {
            if (categoryId.Length == 0)
                return;

            for (int i = buffer.Length - 1; i >= 0; i--)
            {
                var inst = buffer[i];
                if (inst.CategoryId.Length == 0 || !inst.CategoryId.Equals(categoryId))
                    continue;
                if (inst.CategoryLevel < categoryLevel)
                    buffer.RemoveAt(i);
            }
        }

        private static int IndexOf(ref DynamicBuffer<TimedEffectInstance> buffer, in FixedString64Bytes effectId)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].EffectId.Equals(effectId))
                    return i;
            }
            return -1;
        }
    }
}
