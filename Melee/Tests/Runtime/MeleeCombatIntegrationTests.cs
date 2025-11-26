using System.Collections.Generic;
using Framework.Buffs.Components;
using Framework.Core.Base;
using Framework.Damage.Components;
using Framework.Damage.Requests;
using Framework.Melee.Blobs;
using Framework.Melee.Components;
using Framework.Melee.Runtime.SystemGroups;
using Framework.Melee.Runtime.Utilities;
using Framework.Resources.Components;
using Framework.Resources.Requests;
using Framework.TimedEffect.Requests;
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Framework.Melee.Tests.Runtime
{
    public sealed class MeleeCombatIntegrationTests
    {
        private World _world;
        private EntityManager _em;
        private BeginSimulationEntityCommandBufferSystem _begin;
        private RequestsSystemGroup _requests;
        private RuntimeSystemGroup _runtime;
        private ResolutionSystemGroup _resolution;
        private EndSimulationEntityCommandBufferSystem _end;
        private readonly List<BlobAssetReference<MeleeWeaponDefBlob>> _weaponDefs = new();
        private readonly List<BlobAssetReference<MeleeProcTableBlob>> _procTables = new();

        [SetUp]
        public void SetUp()
        {
            _world = new World(nameof(MeleeCombatIntegrationTests));
            InitializeWorld(_world);
            _em = _world.EntityManager;
            CacheSystemGroups();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var weapon in _weaponDefs)
            {
                if (weapon.IsCreated)
                    weapon.Dispose();
            }
            _weaponDefs.Clear();

            foreach (var table in _procTables)
            {
                if (table.IsCreated)
                    table.Dispose();
            }
            _procTables.Clear();

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
        public void MultiAttackResolverQueuesChainRequests()
        {
            var weapon = BuildMultiAttackWeapon();
            _weaponDefs.Add(weapon);

            var attacker = CreateAttacker(weapon, new MeleeStatSnapshot());
            var target = CreateTarget();

            MeleeRequestFactory.QueueAttack(ref _em, attacker, new FixedString32Bytes("MainHand"), new float3(0, 0, 1), target);

            StepWorld(); // initial swing

            var requests = _em.GetBuffer<MeleeAttackRequestElement>(attacker);
            Assert.AreEqual(1, requests.Length, "Resolver should queue one chain request for double attack.");
            Assert.AreEqual(1, requests[0].ChainDepth);
            Assert.IsTrue((requests[0].Flags & MeleeRequestFlags.MultiAttackChain) != 0, "Chain request flag should be set.");

            StepWorld(); // process chain request

            var contextQuery = _em.CreateEntityQuery(ComponentType.ReadOnly<MeleeCastContext>());
            using var contexts = contextQuery.ToComponentDataArray<MeleeCastContext>(Allocator.Temp);
            int depth0 = 0;
            int depth1 = 0;
            for (int i = 0; i < contexts.Length; i++)
            {
                if (contexts[i].ChainDepth == 0)
                    depth0++;
                if (contexts[i].ChainDepth == 1)
                    depth1++;
            }

            Assert.GreaterOrEqual(depth0, 1, "Original swing context missing.");
            Assert.GreaterOrEqual(depth1, 1, "Chain swing context missing.");
        }

        [Test]
        public void ProcsRouteThroughFactories()
        {
            var weapon = BuildWeaponWithProc(MeleeProcPayloadKind.ExtraDamage, new FixedString64Bytes("Proc.ExtraDamage"));
            _weaponDefs.Add(weapon);

            var dotTable = BuildProcTable(new FixedString32Bytes("Proc.Dot"), MeleeProcPayloadKind.DamageOverTime, new FixedString64Bytes("DOT.Test"));
            var attacker = CreateAttacker(weapon, new MeleeStatSnapshot());
            var target = CreateTarget();

            MeleeProcUtility.AddEquipmentProc(ref _em, attacker, new FixedString64Bytes("Buff.RazorStrikes"), dotTable);

            MeleeRequestFactory.QueueAttack(ref _em, attacker, new FixedString32Bytes("MainHand"), new float3(0, 0, 1), target);

            StepWorld(4);

            Assert.IsTrue(_em.HasBuffer<DamageRequest>(target));
            var damageBuffer = _em.GetBuffer<DamageRequest>(target);
            Assert.GreaterOrEqual(damageBuffer.Length, 2, "Expected base damage and extra damage proc.");

            Assert.IsTrue(_em.HasBuffer<TimedEffectRequest>(target));
            var timedRequests = _em.GetBuffer<TimedEffectRequest>(target);
            Assert.GreaterOrEqual(timedRequests.Length, 1, "Expected DOT timed effect from proc.");
        }

        [Test]
        public void WardsAndShieldsConsumeBeforeDamage()
        {
            var weapon = BuildSimpleWeapon();
            _weaponDefs.Add(weapon);

            var attacker = CreateAttacker(weapon, new MeleeStatSnapshot());
            var target = CreateTarget();

            var wards = _em.AddBuffer<MeleeWardStateElement>(target);
            wards.Add(new MeleeWardStateElement
            {
                WardId = new FixedString32Bytes("Test.Ward"),
                BuffId = new FixedString64Bytes("Buff.Ward"),
                RemainingActivations = 1,
                MaxActivations = 1,
                ExpireTime = 0,
                AbsorbFlat = 100f,
                TriggerOnZeroDamage = 1
            });

            var shields = _em.AddBuffer<DamageShieldStateElement>(target);
            shields.Add(new DamageShieldStateElement
            {
                ShieldId = new FixedString32Bytes("Test.Shield"),
                BuffId = new FixedString64Bytes("Buff.Shield"),
                RemainingActivations = 1,
                MaxActivations = 1,
                ExpireTime = 0,
                NextReadyTime = 0,
                InternalCooldownSeconds = 0,
                TriggerOnZeroDamage = 1,
                PayloadKind = (byte)MeleeProcPayloadKind.ExtraDamage,
                TargetMode = (byte)MeleeProcTargetMode.Target,
                PayloadRef = new FixedString64Bytes("Shield.ExtraHit"),
                ArgInt0 = 5
            });

            MeleeRequestFactory.QueueAttack(ref _em, attacker, new FixedString32Bytes("MainHand"), new float3(0, 0, 1), target);

            StepWorld(4);

            Assert.IsTrue(_em.HasBuffer<DamageRequest>(target));
            var damageBuffer = _em.GetBuffer<DamageRequest>(target);
            Assert.AreEqual(1, damageBuffer.Length, "Only shield retaliation damage should be applied.");
            Assert.AreEqual(5, damageBuffer[0].Packet.Amount);

            wards = _em.GetBuffer<MeleeWardStateElement>(target);
            Assert.AreEqual(0, wards[0].RemainingActivations);

            shields = _em.GetBuffer<DamageShieldStateElement>(target);
            Assert.AreEqual(0, shields[0].RemainingActivations);

            var telemetryQuery = _em.CreateEntityQuery(ComponentType.ReadOnly<MeleeTelemetryEvent>());
            using var telemetryEntities = telemetryQuery.ToEntityArray(Allocator.Temp);
            Assert.Greater(telemetryEntities.Length, 0, "Telemetry buffer entity not found.");
            var telemetryBuffer = _em.GetBuffer<MeleeTelemetryEvent>(telemetryEntities[0]);
            bool wardLogged = false;
            bool shieldLogged = false;
            for (int i = 0; i < telemetryBuffer.Length; i++)
            {
                wardLogged |= telemetryBuffer[i].EventType == MeleeTelemetryEventType.WardConsumed;
                shieldLogged |= telemetryBuffer[i].EventType == MeleeTelemetryEventType.DamageShieldTriggered;
            }
            Assert.IsTrue(wardLogged, "Ward consumption telemetry missing.");
            Assert.IsTrue(shieldLogged, "Damage shield telemetry missing.");
        }

        #region Helpers

        private BlobAssetReference<MeleeWeaponDefBlob> BuildMultiAttackWeapon()
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var weapon = ref builder.ConstructRoot<MeleeWeaponDefBlob>();
            weapon.WeaponId = new FixedString64Bytes("Test.MultiAttackWeapon");
            weapon.BaseDamage = new DamagePacket { Amount = 10, CritMult = 1f };
            weapon.WindupSeconds = 0f;
            weapon.ActiveSeconds = 0.01f;
            weapon.RecoverySeconds = 0.01f;
            weapon.Range = 4f;
            weapon.PenetrationCount = 1;
            weapon.StaminaCost = 0;
            weapon.LockoutSeconds = 0.5f;
            weapon.DefaultCleaveArcDegrees = 45f;
            weapon.DefaultCleaveMaxTargets = 1;
            weapon.GuardCost = 0f;
            weapon.MultiAttack = new MeleeMultiAttackConfig
            {
                DoubleChancePercent = 100f,
                TripleChancePercent = 0f,
                FlurryChancePercent = 0f,
                FlurryPerAttackPercent = 0f,
                FlurryMaxExtraAttacks = 0,
                MaxChainDepth = 2,
                ChainLockoutSeconds = 0f,
                ChainDelaySeconds = 0f,
                AreaChancePercent = 0f,
                AreaShape = MeleeChainAttackShape.None,
                AreaArcDegrees = 0f,
                AreaMaxTargets = 0,
                AreaRadius = 0f
            };
            builder.Allocate(ref weapon.ProcEntries, 0);
            var blob = builder.CreateBlobAssetReference<MeleeWeaponDefBlob>(Allocator.Persistent);
            builder.Dispose();
            return blob;
        }

        private BlobAssetReference<MeleeWeaponDefBlob> BuildWeaponWithProc(MeleeProcPayloadKind payloadKind, FixedString64Bytes payloadRef)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var weapon = ref builder.ConstructRoot<MeleeWeaponDefBlob>();
            weapon.WeaponId = new FixedString64Bytes("Test.ProcWeapon");
            weapon.BaseDamage = new DamagePacket { Amount = 20, CritMult = 1f };
            weapon.WindupSeconds = 0f;
            weapon.ActiveSeconds = 0.01f;
            weapon.RecoverySeconds = 0.01f;
            weapon.Range = 3f;
            weapon.PenetrationCount = 1;
            weapon.StaminaCost = 0;
            weapon.LockoutSeconds = 0.4f;
            weapon.DefaultCleaveArcDegrees = 30f;
            weapon.DefaultCleaveMaxTargets = 1;
            weapon.GuardCost = 0f;
            weapon.MultiAttack = default;

            var entries = builder.Allocate(ref weapon.ProcEntries, 1);
            entries[0] = new MeleeProcEntry
            {
                ProcId = new FixedString32Bytes("Proc.Extra"),
                ChancePercent = 100f,
                InternalCooldownSeconds = 0f,
                MaxTriggers = 0,
                WindowSeconds = 0f,
                PayloadKind = payloadKind,
                PayloadRef = payloadRef,
                TargetMode = MeleeProcTargetMode.Target,
                ChargeMode = MeleeProcChargeMode.PerTriggerSuccess,
                MaxActivations = 0,
                DurationSeconds = 0f,
                TriggerOnZeroDamage = 1,
                MeleeOnly = 1,
                SourceKeyHint = default,
                Payload = default
            };

            var blob = builder.CreateBlobAssetReference<MeleeWeaponDefBlob>(Allocator.Persistent);
            builder.Dispose();
            return blob;
        }

        private BlobAssetReference<MeleeWeaponDefBlob> BuildSimpleWeapon()
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var weapon = ref builder.ConstructRoot<MeleeWeaponDefBlob>();
            weapon.WeaponId = new FixedString64Bytes("Test.SimpleWeapon");
            weapon.BaseDamage = new DamagePacket { Amount = 30, CritMult = 1f };
            weapon.WindupSeconds = 0f;
            weapon.ActiveSeconds = 0.01f;
            weapon.RecoverySeconds = 0.01f;
            weapon.Range = 3f;
            weapon.PenetrationCount = 1;
            weapon.StaminaCost = 0;
            weapon.LockoutSeconds = 0.4f;
            weapon.DefaultCleaveArcDegrees = 30f;
            weapon.DefaultCleaveMaxTargets = 1;
            weapon.GuardCost = 0f;
            builder.Allocate(ref weapon.ProcEntries, 0);
            var blob = builder.CreateBlobAssetReference<MeleeWeaponDefBlob>(Allocator.Persistent);
            builder.Dispose();
            return blob;
        }

        private BlobAssetReference<MeleeProcTableBlob> BuildProcTable(FixedString32Bytes procId, MeleeProcPayloadKind payloadKind, FixedString64Bytes payloadRef)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var table = ref builder.ConstructRoot<MeleeProcTableBlob>();
            var entries = builder.Allocate(ref table.Entries, 1);
            entries[0] = new MeleeProcEntry
            {
                ProcId = procId,
                ChancePercent = 100f,
                InternalCooldownSeconds = 0f,
                MaxTriggers = 0,
                WindowSeconds = 0f,
                PayloadKind = payloadKind,
                PayloadRef = payloadRef,
                TargetMode = MeleeProcTargetMode.Target,
                ChargeMode = MeleeProcChargeMode.PerTriggerSuccess,
                MaxActivations = 0,
                DurationSeconds = 0f,
                TriggerOnZeroDamage = 1,
                MeleeOnly = 1,
                SourceKeyHint = default,
                Payload = default
            };
            var blob = builder.CreateBlobAssetReference<MeleeProcTableBlob>(Allocator.Persistent);
            builder.Dispose();
            _procTables.Add(blob);
            return blob;
        }

        private Entity CreateAttacker(BlobAssetReference<MeleeWeaponDefBlob> weaponDef, MeleeStatSnapshot stats)
        {
            var entity = _em.CreateEntity();
            _em.AddComponentData(entity, LocalTransform.Identity);
            _em.AddComponentData(entity, new MeleeLockout());
            _em.AddComponentData(entity, new MeleeRequestSequence { NextId = 1 });
            _em.AddComponentData(entity, stats);
            _em.AddBuffer<MeleeAttackRequestElement>(entity);
            _em.AddBuffer<MeleeVictimElement>(entity);
            _em.AddBuffer<MeleeRiposteRequestElement>(entity);
            _em.AddBuffer<ResourceRequest>(entity);
            _em.AddComponentData(entity, new Stamina { Current = 100, Max = 100, RegenPerSecond = 0, RegenAccumulator = 0 });

            var slots = _em.AddBuffer<MeleeWeaponSlotElement>(entity);
            slots.Add(new MeleeWeaponSlotElement
            {
                Definition = weaponDef,
                SlotId = new FixedString32Bytes("MainHand"),
                Enabled = 1,
                SwingOrder = 0,
                FamilyLockoutSeconds = 0
            });

            return entity;
        }

        private Entity CreateTarget()
        {
            var entity = _em.CreateEntity();
            _em.AddComponentData(entity, LocalTransform.Identity);
            _em.AddComponentData(entity, new Health { Current = 1000, Max = 1000, RegenPerSecond = 0, RegenAccumulator = 0f });
            _em.AddComponentData(entity, new MeleeDefenseTuning());
            _em.AddComponentData(entity, new MeleeDefenseWindowState());
            return entity;
        }

        private void StepWorld(int frames = 1)
        {
            for (int i = 0; i < frames; i++)
            {
                double elapsed = _world.Time.ElapsedTime + 1d / 60d;
                _world.SetTime(new TimeData(elapsed, 1f / 60f));

                _begin?.Update();
                _requests?.Update();
                _runtime?.Update();
                _resolution?.Update();
                _end?.Update();
            }
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

        #endregion
    }
}
