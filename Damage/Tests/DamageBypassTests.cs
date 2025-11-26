using Framework.Core.Base;
using Framework.Damage.Components;
using Framework.Damage.Factory;
using Framework.Damage.Policies;
using Framework.Damage.Requests;
using Framework.Resources.Components;
using Framework.Resources.Factory;
using NUnit.Framework;
using Unity.Core;
using Unity.Entities;

namespace Framework.Damage.Tests
{
    public sealed class DamageBypassTests
    {
        private const int TargetHealth = 200;

        private World _world;
        private EntityManager _em;
        private BeginSimulationEntityCommandBufferSystem _begin;
        private RequestsSystemGroup _requests;
        private RuntimeSystemGroup _runtime;
        private ResolutionSystemGroup _resolution;
        private EndSimulationEntityCommandBufferSystem _end;

        [SetUp]
        public void SetUp()
        {
            _world = new World("DamageBypassTests");
            InitializeWorld(_world);
            _em = _world.EntityManager;
            CacheSystemGroups();
        }

        [TearDown]
        public void TearDown()
        {
            if (_world != null && _world.IsCreated)
                _world.Dispose();
            _world = null;
            _em = default;
            _begin = null;
            _requests = null;
            _runtime = null;
            _resolution = null;
            _end = null;
        }

        [Test]
        public void IgnoreArmor_BypassesAllArmorMitigation()
        {
            var target = SpawnTarget(armor: 999, resistPercent: 0f);
            QueueDamage(target, amount: 60, ignoreArmor: true, ignoreResist: false);

            StepWorld(1);

            AssertDamageDelta(target, expectedDelta: 60, "IgnoreArmor should zero out the armor contribution.");
        }

        [Test]
        public void IgnoreResist_BypassesResistMitigation()
        {
            var target = SpawnTarget(armor: 0, resistPercent: 0.8f);
            QueueDamage(target, amount: 60, ignoreArmor: false, ignoreResist: true);

            StepWorld(1);

            AssertDamageDelta(target, expectedDelta: 60, "IgnoreResist should remove resist percent reductions.");
        }

        [Test]
        public void IgnoreArmorAndResist_BypassesAllMitigation()
        {
            var target = SpawnTarget(armor: 999, resistPercent: 0.8f);
            QueueDamage(target, amount: 60, ignoreArmor: true, ignoreResist: true);

            StepWorld(1);

            AssertDamageDelta(target, expectedDelta: 60, "Bypassing both flags should apply full raw damage.");
        }

        [Test]
        public void NoBypass_UsesDamagePolicyMitigation()
        {
            const int raw = 60;
            const int armor = 999;
            const float resist = 0.8f;

            var target = SpawnTarget(armor, resist);
            QueueDamage(target, amount: raw, ignoreArmor: false, ignoreResist: false);

            StepWorld(1);

            var expected = DamagePolicy.Mitigate(raw, armor, resist);
            AssertDamageDelta(target, expected, "Without bypass flags the policy curve should run unchanged.");
        }

        private void InitializeWorld(World world)
        {
            if (world == null || !world.IsCreated)
                return;

            world.GetOrCreateSystemManaged<SimulationSystemGroup>();
            world.GetOrCreateSystemManaged<RequestsSystemGroup>();
            world.GetOrCreateSystemManaged<ResolutionSystemGroup>();
            world.GetOrCreateSystemManaged<RuntimeSystemGroup>();
            world.GetOrCreateSystemManaged<TelemetrySystemGroup>();
            world.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>();
            world.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            SubsystemBootstrap.InstallAll(world);
        }

        private void CacheSystemGroups()
        {
            _begin = _world.GetExistingSystemManaged<BeginSimulationEntityCommandBufferSystem>();
            _requests = _world.GetExistingSystemManaged<RequestsSystemGroup>();
            _runtime = _world.GetExistingSystemManaged<RuntimeSystemGroup>();
            _resolution = _world.GetExistingSystemManaged<ResolutionSystemGroup>();
            _end = _world.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>();
        }

        private Entity SpawnTarget(int armor, float resistPercent)
        {
            var entity = _em.CreateEntity();
            ResourceFactory.InitHealth(ref _em, entity, TargetHealth, TargetHealth);

            if (!_em.HasComponent<Damageable>(entity))
                _em.AddComponentData(entity, new Damageable { Armor = armor, ResistPercent = resistPercent });
            else
                _em.SetComponentData(entity, new Damageable { Armor = armor, ResistPercent = resistPercent });

            if (!_em.HasBuffer<DamageRequest>(entity))
                _em.AddBuffer<DamageRequest>(entity);

            return entity;
        }

        private void QueueDamage(Entity target, int amount, bool ignoreArmor, bool ignoreResist)
        {
            var packet = new DamagePacket
            {
                Amount = amount,
                IgnoreArmor = (byte)(ignoreArmor ? 1 : 0),
                IgnoreResist = (byte)(ignoreResist ? 1 : 0),
                IgnoreSnapshotModifiers = 1
            };

            DamageFactory.EnqueueDamage(ref _em, target, packet);
        }

        private void StepWorld(int frames)
        {
            double elapsed = _world.Time.ElapsedTime;
            for (int i = 0; i < frames; i++)
            {
                elapsed += 1f / 60f;
                _world.SetTime(new TimeData(elapsed, 1f / 60f));

                _begin?.Update();
                _requests?.Update();
                _runtime?.Update();
                _resolution?.Update();
                _end?.Update();
            }
        }

        private void AssertDamageDelta(Entity target, int expectedDelta, string message)
        {
            var health = _em.GetComponentData<Health>(target);
            var delta = TargetHealth - health.Current;
            Assert.AreEqual(expectedDelta, delta, message);
        }
    }
}
