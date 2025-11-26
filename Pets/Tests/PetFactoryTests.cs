using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Framework.Contracts.Intents;
using Framework.Core.Components;
using Framework.Pets.Components;
using Framework.Pets.Contracts;
using Framework.Pets.Content;
using Framework.Pets.Factory;
using Framework.Pets.Runtime;
using Framework.Pets.Systems;
using Framework.TimedEffect.Events;
using Unity.Mathematics;

namespace Framework.Pets.Tests
{
    public class PetFactoryTests
    {
        private World _world;
        private EntityManager _em;
        private Framework.Core.Base.ResolutionSystemGroup _resolution;

        [SetUp]
        public void SetUp()
        {
            _world = new World("PetFactoryTests");
            _world.CreateSystemManaged<Framework.Core.Base.RequestsSystemGroup>();
            _resolution = _world.CreateSystemManaged<Framework.Core.Base.ResolutionSystemGroup>();
            _world.CreateSystemManaged<Framework.Core.Base.RuntimeSystemGroup>();
            _em = _world.EntityManager;
            var manifest = new PetSubsystemManifest();
            manifest.Register(_world, _em);
            PetSampleContent.RegisterDefaults();
        }

        [TearDown]
        public void TearDown()
        {
            if (_world != null && _world.IsCreated)
                _world.Dispose();
            _em = default;
        }

        [Test]
        public void SummonCreatesPetWithAI()
        {
            var owner = CreateOwner();
            PetFactory.Summon(ref _em, owner, Entity.Null, (FixedString64Bytes)"wolf", 1, 2f, default, 1);

            var query = _em.CreateEntityQuery(ComponentType.ReadOnly<PetTag>());
            Assert.That(query.CalculateEntityCount(), Is.EqualTo(1));
            var pet = query.GetSingletonEntity();
            Assert.IsTrue(_em.HasComponent<MoveIntent>(pet));
            Assert.IsTrue(_em.HasComponent<CastIntent>(pet));
            query.Dispose();
        }

        [Test]
        public void LimitPolicyHonorsMaxCount()
        {
            var owner = CreateOwner();
            PetFactory.Summon(ref _em, owner, Entity.Null, (FixedString64Bytes)"wolf", 1, 2f, default, 1);
            PetFactory.Summon(ref _em, owner, Entity.Null, (FixedString64Bytes)"wolf", 1, 2f, default, 1);
            var query = _em.CreateEntityQuery(ComponentType.ReadOnly<PetTag>());
            Assert.That(query.CalculateEntityCount(), Is.EqualTo(1));
            query.Dispose();
        }

        [Test]
        public void LifetimeRemovalDespawnsPet()
        {
            var owner = CreateOwner();
            PetFactory.Summon(ref _em, owner, Entity.Null, (FixedString64Bytes)"imp", 1, 2f, default, 1);
            var query = _em.CreateEntityQuery(ComponentType.ReadOnly<PetLifetimeTag>());
            var pet = query.GetSingletonEntity();
            query.Dispose();

            if (!_em.HasBuffer<TimedEffectEvent>(pet))
                _em.AddBuffer<TimedEffectEvent>(pet);
            var events = _em.GetBuffer<TimedEffectEvent>(pet);
            events.Add(new TimedEffectEvent
            {
                Kind = TimedEffectEventKind.Removed,
                EffectId = _em.GetComponentData<PetLifetimeTag>(pet).EffectId,
                Target = pet,
                Source = owner
            });

            _resolution.Update();

            var petsLeft = _em.CreateEntityQuery(ComponentType.ReadOnly<PetTag>()).CalculateEntityCount();
            Assert.AreEqual(0, petsLeft);
        }

        private Entity CreateOwner()
        {
            var owner = _em.CreateEntity();
            _em.AddComponentData(owner, new Position { Value = new float2(1f, 1f) });
            _em.AddBuffer<PetCommandRequest>(owner);
            return owner;
        }
    }
}
