using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Framework.Core.Components;
using Framework.Pets.Components;
using Framework.Pets.Contracts;
using Framework.Pets.Content;
using Framework.Pets.Drivers;
using Framework.Pets.Factory;
using Framework.Pets.Runtime;
using ContractsAIAgentTarget = Framework.Contracts.AI.AIAgentTarget;

namespace Framework.Pets.Tests
{
    public class PetCommandTests
    {
        private World _world;
        private EntityManager _em;
        private Framework.Core.Base.RequestsSystemGroup _requests;

        [SetUp]
        public void SetUp()
        {
            _world = new World("PetCommandTests");
            _world.CreateSystemManaged<SimulationSystemGroup>();
            _requests = _world.CreateSystemManaged<Framework.Core.Base.RequestsSystemGroup>();
            _world.CreateSystemManaged<Framework.Core.Base.ResolutionSystemGroup>();
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
            _requests = null;
        }

        [Test]
        public void AttackCommandSetsTarget()
        {
            var owner = CreateOwner();
            var pet = SummonPet(owner, (FixedString64Bytes)"wolf");
            var target = CreateTargetEntity(new float2(12f, 8f));

            PetDriver.CommandAttackAll(ref _em, owner, target);
            _requests.Update();

            Assert.That(_em.HasComponent<ContractsAIAgentTarget>(pet), Is.True);
            var agentTarget = _em.GetComponentData<ContractsAIAgentTarget>(pet);
            Assert.AreEqual(target, agentTarget.Value);
            Assert.AreEqual(1, agentTarget.Visibility);
        }

        [Test]
        public void BackOffClearsTargetAndResetsAnchor()
        {
            var owner = CreateOwner();
            var pet = SummonPet(owner, (FixedString64Bytes)"wolf");
            var target = CreateTargetEntity(new float2(6f, -3f));

            PetDriver.CommandAttackAll(ref _em, owner, target);
            _requests.Update();

            PetDriver.CommandBackOffAll(ref _em, owner);
            _requests.Update();

            var agentTarget = _em.GetComponentData<ContractsAIAgentTarget>(pet);
            Assert.AreEqual(Entity.Null, agentTarget.Value);
            Assert.AreEqual(0, agentTarget.Visibility);

            var guard = _em.GetComponentData<PetGuardAnchor>(pet);
            Assert.AreEqual(owner, guard.AnchorEntity);
        }

        private Entity CreateOwner()
        {
            var owner = _em.CreateEntity();
            _em.AddComponentData(owner, new Position { Value = new float2(1f, 1f) });
            _em.AddBuffer<PetCommandRequest>(owner);
            return owner;
        }

        private Entity SummonPet(in Entity owner, in FixedString64Bytes petId)
        {
            PetFactory.Summon(ref _em, owner, Entity.Null, petId, 1, 2f, default, 1);
            using var query = _em.CreateEntityQuery(ComponentType.ReadOnly<PetTag>());
            return query.GetSingletonEntity();
        }

        private Entity CreateTargetEntity(in float2 position)
        {
            var target = _em.CreateEntity();
            _em.AddComponentData(target, new Position { Value = position });
            return target;
        }
    }
}
