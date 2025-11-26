using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Framework.Resources.Components;
using Framework.Resources.Factory;
using Framework.Spells.Content;
using Framework.Spells.Pipeline.Components;
using Framework.Spells.Pipeline.Config;
using Framework.Spells.Pipeline.Events;
using Framework.Spells.Pipeline.Systems;
using Framework.Spells.Requests;
using Framework.Spells.Spellbook.Components;

namespace Framework.Spells.Tests
{
    public class SpellCastPipelineTests
    {
        World _world;
        SpellPipelineSystemGroup _pipeline;
        BeginSimulationEntityCommandBufferSystem _begin;
        EndSimulationEntityCommandBufferSystem _end;
        double _elapsed;
        EntityManager Em => _world.EntityManager;

        [SetUp]
        public void SetUp()
        {
            _world = new World("SpellCastPipelineTests");
            _world.CreateSystemManaged<SimulationSystemGroup>();
            _world.CreateSystemManaged<Framework.Core.Base.RequestsSystemGroup>();
            _world.CreateSystemManaged<Framework.Core.Base.ResolutionSystemGroup>();
            _world.CreateSystemManaged<Framework.Core.Base.RuntimeSystemGroup>();
            _begin = _world.CreateSystemManaged<BeginSimulationEntityCommandBufferSystem>();
            _end = _world.CreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            var manifest = new Framework.Spells.Runtime.SpellSubsystemManifest();
            manifest.Register(_world, _world.EntityManager);
            _pipeline = _world.GetExistingSystemManaged<SpellPipelineSystemGroup>();
            _elapsed = 0;
            OverrideCastConfig();
        }

        [TearDown]
        public void TearDown()
        {
            if (_world != null && _world.IsCreated)
                _world.Dispose();
            SpellDefinitionCatalog.ClearAll();
        }

        [Test]
        public void InterruptChargesConfiguredCost()
        {
            var spellId = new FixedString64Bytes("interrupt-test");
            RegisterSpell(spellId, cost: 100, castTime: 1.2f);
            var caster = CreateCasterWithMana(200, spellId);
            var target = Em.CreateEntity();
            EnqueueRequest(caster, target, spellId);

            Tick(0f);
            Tick(0f);
            var cast = GetActiveCast();
            Assert.AreNotEqual(Entity.Null, cast);

            Em.AddComponentData(cast, new SpellInterruptRequest { Source = target, Reason = new FixedString64Bytes("stagger") });

            Tick(0f);
            Tick(0f);

            var mana = Em.GetComponentData<Mana>(caster).Current;
            Assert.AreEqual(150, mana, "Interrupt should charge 50% of the 100 mana cost");

            Assert.That(Em.CreateEntityQuery(typeof(SpellInterruptedEvent)).CalculateEntityCount(), Is.EqualTo(1));
            Assert.AreEqual(new FixedString64Bytes("stagger"), GetEventReason<SpellInterruptedEvent>());
            FlushEvents();
            Assert.AreEqual(0, Em.CreateEntityQuery(typeof(SpellCastContext)).CalculateEntityCount());
        }

        [Test]
        public void FizzleConsumesFizzlePercentAndEmitsEvent()
        {
            var spellId = new FixedString64Bytes("fizzle-test");
            RegisterSpell(spellId, cost: 60, castTime: 0f);
            var caster = CreateCasterWithMana(150, spellId);
            var target = Em.CreateEntity();
            EnqueueRequest(caster, target, spellId);

            Tick(0f);
            Tick(0f);
            var cast = GetActiveCast();
            Assert.AreNotEqual(Entity.Null, cast, "Cast entity should exist before issuing a fizzle request.");
            Em.AddComponentData(cast, new SpellFizzleRequest { Reason = new FixedString64Bytes("silence") });

            Tick(0f);
            Tick(0f);

            var mana = Em.GetComponentData<Mana>(caster).Current;
            Assert.AreEqual(144, mana, "Fizzle should consume ceil(60 * 0.1) = 6 mana");

            Assert.That(Em.CreateEntityQuery(typeof(SpellFizzledEvent)).CalculateEntityCount(), Is.EqualTo(1));
            Assert.AreEqual(new FixedString64Bytes("silence"), GetEventReason<SpellFizzledEvent>());
            FlushEvents();
        }

        void Tick(float deltaTime)
        {
            _elapsed += deltaTime;
            _world.SetTime(new TimeData(_elapsed, deltaTime));
            _begin.Update();
            _pipeline.Update();
            _end.Update();
        }

        Entity CreateCasterWithMana(int mana, FixedString64Bytes spellId)
        {
            var em = _world.EntityManager;
            var caster = em.CreateEntity();
            ResourceFactory.InitMana(ref em, caster, mana, mana);
            var slots = em.AddBuffer<SpellSlot>(caster);
            slots.Add(new SpellSlot { SpellId = spellId });
            em.AddBuffer<SpellCastRequest>(caster);
            return caster;
        }

        void RegisterSpell(FixedString64Bytes spellId, int cost, float castTime)
        {
            var definition = new SpellDefinition
            {
                Id = spellId,
                CategoryId = new FixedString32Bytes("fire"),
                CategoryLevel = 1,
                SpellLevel = 1,
                Rank = SpellRank.Apprentice,
                Costs = new[] { new SpellCost { Resource = new FixedString64Bytes("Mana"), Amount = cost } },
                CastTime = castTime,
                Cooldown = 1f,
                Range = 25f,
                Targeting = SpellTargeting.Enemy,
                Blocks = Array.Empty<EffectBlock>(),
                Flags = SpellDefinitionFlags.None,
                InterruptChargePercentOverride = 0f,
                FizzleChargePercentOverride = 0f
            };
            SpellDefinitionCatalog.Register(definition);
        }

        void EnqueueRequest(Entity caster, Entity target, FixedString64Bytes spellId)
        {
            var buffer = Em.GetBuffer<SpellCastRequest>(caster);
            buffer.Add(new SpellCastRequest { Caster = caster, Target = target, SpellKey = spellId });
        }

        Entity GetActiveCast()
        {
            var query = Em.CreateEntityQuery(typeof(SpellCastContext));
            var result = query.IsEmptyIgnoreFilter ? Entity.Null : query.GetSingletonEntity();
            query.Dispose();
            return result;
        }

        void FlushEvents()
        {
            DestroyEvents<SpellBeganEvent>();
            DestroyEvents<SpellInterruptedEvent>();
            DestroyEvents<SpellFizzledEvent>();
            DestroyEvents<SpellResolvedEvent>();
        }

        void DestroyEvents<T>() where T : unmanaged, IComponentData
        {
            var query = Em.CreateEntityQuery(ComponentType.ReadOnly<T>());
            if (!query.IsEmptyIgnoreFilter)
                Em.DestroyEntity(query);
            query.Dispose();
        }

        FixedString64Bytes GetEventReason<T>() where T : unmanaged, IComponentData
        {
            var query = Em.CreateEntityQuery(ComponentType.ReadOnly<T>(), ComponentType.ReadOnly<SpellCastEventPayload>());
            if (query.IsEmptyIgnoreFilter)
            {
                query.Dispose();
                return default;
            }

            var entity = query.GetSingletonEntity();
            var payload = Em.GetBuffer<SpellCastEventPayload>(entity);
            var reason = payload.Length > 0 ? payload[0].Reason : default;
            query.Dispose();
            return reason;
        }

        void OverrideCastConfig()
        {
            var em = _world.EntityManager;
            var query = em.CreateEntityQuery(ComponentType.ReadOnly<CastGlobalConfigSingleton>());
            if (query.IsEmptyIgnoreFilter)
            {
                query.Dispose();
                return;
            }

            var entity = query.GetSingletonEntity();
            var singleton = em.GetComponentData<CastGlobalConfigSingleton>(entity);
            if (singleton.Reference.IsCreated)
                singleton.Reference.Dispose();

            var builder = new Unity.Entities.BlobBuilder(Unity.Collections.Allocator.Temp);
            ref var cfg = ref builder.ConstructRoot<CastGlobalConfig>();
            var defaults = CastGlobalConfigSingleton.DefaultValues;
            defaults.AllowPartialRefundOnPreResolve = 1;
            cfg = defaults;
            var blob = builder.CreateBlobAssetReference<CastGlobalConfig>(Unity.Collections.Allocator.Persistent);
            builder.Dispose();

            em.SetComponentData(entity, new CastGlobalConfigSingleton { Reference = blob });
            query.Dispose();
        }
    }
}
