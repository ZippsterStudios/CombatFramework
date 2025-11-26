using Framework.Core.Base;
using Framework.Melee.Components;
using Framework.Melee.Runtime.SystemGroups;
using Framework.Resources.Components;
using Framework.Resources.Requests;
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Framework.Melee.Tests.Runtime
{
    public sealed class MeleePlanBuilderTests
    {
        private World _world;
        private EntityManager _em;
        private BeginSimulationEntityCommandBufferSystem _begin;
        private RequestsSystemGroup _requests;
        private RuntimeSystemGroup _runtime;
        private ResolutionSystemGroup _resolution;
        private EndSimulationEntityCommandBufferSystem _end;
        private BlobAssetReference<Blobs.MeleeWeaponDefBlob> _weaponDefBlob;

        [SetUp]
        public void SetUp()
        {
            _world = new World("MeleePlanBuilderTests");
            InitializeWorld(_world);
            _em = _world.EntityManager;
            CacheSystemGroups();
            _weaponDefBlob = BuildWeaponDef();
        }

        [TearDown]
        public void TearDown()
        {
            if (_weaponDefBlob.IsCreated)
                _weaponDefBlob.Dispose();

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
        public void PlanBuilderSpawnsSwingEntityForValidRequest()
        {
            var attacker = CreateAttacker();
            var requests = _em.GetBuffer<MeleeAttackRequestElement>(attacker);
            requests.Add(new MeleeAttackRequestElement
            {
                Attacker = attacker,
                WeaponSlot = new FixedString32Bytes("MainHand"),
                AimDirection = new float3(0, 0, 1),
                PreferredTarget = Entity.Null,
                Flags = MeleeRequestFlags.None,
                RequestId = 1
            });

            StepWorld();

            var query = _em.CreateEntityQuery(ComponentType.ReadOnly<MeleeCastContext>());
            Assert.AreEqual(1, query.CalculateEntityCount(), "Plan builder should spawn a swing entity when the request passes validation.");
        }

        private Entity CreateAttacker()
        {
            var attacker = _em.CreateEntity();
            _em.AddComponentData(attacker, LocalTransform.Identity);
            _em.AddComponentData(attacker, new MeleeLockout());
            _em.AddBuffer<MeleeAttackRequestElement>(attacker);
            _em.AddBuffer<MeleeVictimElement>(attacker);
            _em.AddBuffer<MeleeRiposteRequestElement>(attacker);
            _em.AddBuffer<ResourceRequest>(attacker);
            _em.AddComponentData(attacker, new Stamina { Current = 100, Max = 100, RegenPerSecond = 0, RegenAccumulator = 0 });

            var slots = _em.AddBuffer<MeleeWeaponSlotElement>(attacker);
            slots.Add(new MeleeWeaponSlotElement
            {
                Definition = _weaponDefBlob,
                SlotId = new FixedString32Bytes("MainHand"),
                Enabled = 1,
                SwingOrder = 0,
                FamilyLockoutSeconds = 0
            });

            return attacker;
        }

        private BlobAssetReference<Blobs.MeleeWeaponDefBlob> BuildWeaponDef()
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<Blobs.MeleeWeaponDefBlob>();
            root.WeaponId = new FixedString64Bytes("TestSword");
            root.BaseDamage = new Framework.Damage.Components.DamagePacket
            {
                Amount = 50,
                CritMult = 1f
            };
            root.WindupSeconds = 0.1f;
            root.ActiveSeconds = 0.1f;
            root.RecoverySeconds = 0.1f;
            root.Range = 3f;
            root.BaselineArcDegrees = 60f;
            root.PenetrationCount = 1;
            root.StaminaCost = 10;
            root.LockoutSeconds = 0.5f;
            root.DefaultBypassFlags = 0;
            root.DefaultCleaveArcDegrees = 90f;
            root.DefaultCleaveMaxTargets = 3;
            root.GuardCost = 0f;
            builder.Allocate(ref root.ProcEntries, 0);
            var blob = builder.CreateBlobAssetReference<Blobs.MeleeWeaponDefBlob>(Allocator.Persistent);
            builder.Dispose();
            return blob;
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
            world.GetOrCreateSystemManaged<MeleeSystemGroup>();
            Framework.Core.Base.SubsystemBootstrap.InstallAll(world);
        }

        private void CacheSystemGroups()
        {
            _begin = _world.GetExistingSystemManaged<BeginSimulationEntityCommandBufferSystem>();
            _requests = _world.GetExistingSystemManaged<RequestsSystemGroup>();
            _runtime = _world.GetExistingSystemManaged<RuntimeSystemGroup>();
            _resolution = _world.GetExistingSystemManaged<ResolutionSystemGroup>();
            _end = _world.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>();
        }

        private void StepWorld()
        {
            double elapsed = _world.Time.ElapsedTime;
            elapsed += 1f / 60f;
            _world.SetTime(new TimeData(elapsed, 1f / 60f));

            _begin?.Update();
            _requests?.Update();
            _runtime?.Update();
            _resolution?.Update();
            _end?.Update();
        }
    }
}
