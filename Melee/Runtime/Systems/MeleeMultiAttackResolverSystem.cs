using Framework.Melee.Blobs;
using Framework.Melee.Components;
using Framework.Melee.Runtime.SystemGroups;
using Framework.Melee.Runtime.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Melee.Runtime.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(MeleeSystemGroup))]
    [UpdateAfter(typeof(MeleePlanBuilderSystem))]
    [UpdateBefore(typeof(MeleePhaseSystem))]
    public partial struct MeleeMultiAttackResolverSystem : ISystem
    {
        private BufferLookup<MeleeAttackRequestElement> _requestLookup;
        private ComponentLookup<MeleeStatSnapshot> _statLookupRO;

        public void OnCreate(ref SystemState state)
        {
            _requestLookup = state.GetBufferLookup<MeleeAttackRequestElement>();
            _statLookupRO = state.GetComponentLookup<MeleeStatSnapshot>(true);
        }

        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _requestLookup.Update(ref state);
            _statLookupRO.Update(ref state);

            double now = SystemAPI.Time.ElapsedTime;
            float delta = SystemAPI.Time.DeltaTime;
            uint frameToken = (uint)math.max(0, math.floor(now / math.max(delta, 1e-6f)));

            foreach (var (contextRef, entity) in SystemAPI.Query<RefRW<MeleeCastContext>>().WithEntityAccess())
            {
                ref var context = ref contextRef.ValueRW;
                if (context.MultiAttackResolved != 0 || !context.Definition.IsCreated)
                    continue;

                var attacker = context.Attacker;
                if (attacker == Entity.Null || !_requestLookup.HasBuffer(attacker))
                {
                    context.MultiAttackResolved = 1;
                    continue;
                }

                ref var weaponDef = ref context.Definition.Value;
                var config = weaponDef.MultiAttack;

                byte maxDepthWeapon = config.MaxChainDepth;
                byte maxDepthStats = 0;
                if (_statLookupRO.HasComponent(attacker))
                    maxDepthStats = _statLookupRO[attacker].MultiMaxChainDepth;

                int allowedDepth = math.max((int)maxDepthWeapon, (int)maxDepthStats);
                if (allowedDepth > 0 && context.ChainDepth >= allowedDepth)
                {
                    context.MultiAttackResolved = 1;
                    continue;
                }

                var requests = _requestLookup[attacker];
                var rng = MeleeDeterministicRng.FromRaw(context.DeterministicSeed);
                var stats = _statLookupRO.HasComponent(attacker) ? _statLookupRO[attacker] : default;

                float doubleChance = math.clamp(config.DoubleChancePercent + stats.MultiDoubleChance, 0f, 100f);
                float tripleChance = math.clamp(config.TripleChancePercent + stats.MultiTripleChance, 0f, 100f);
                float flurryChance = math.clamp(config.FlurryChancePercent + stats.MultiFlurryChance, 0f, 100f);
                float flurryPerAttack = math.clamp(config.FlurryPerAttackPercent + stats.MultiFlurryPerAttack, 0f, 100f);
                int flurryMax = math.max((int)config.FlurryMaxExtraAttacks, stats.MultiFlurryMaxExtra);
                float areaChance = math.clamp(config.AreaChancePercent + stats.MultiAreaChance, 0f, 100f);
                float chainLockoutSeconds = stats.MultiChainLockoutSeconds > 0f ? stats.MultiChainLockoutSeconds : config.ChainLockoutSeconds;
                float chainDelaySeconds = stats.MultiChainDelaySeconds > 0f ? stats.MultiChainDelaySeconds : config.ChainDelaySeconds;

                byte nextDepth = (byte)(context.ChainDepth + 1);
                if (allowedDepth > 0 && nextDepth > allowedDepth)
                {
                    context.MultiAttackResolved = 1;
                    context.DeterministicSeed = rng.SerializeState();
                    continue;
                }

                var chainRequests = new NativeList<ChainRequestDescriptor>(Allocator.Temp);
                try
                {
                    bool triple = tripleChance > 0f && rng.RollPercent(tripleChance);
                    if (triple)
                    {
                        chainRequests.Add(new ChainRequestDescriptor { Shape = MeleeChainAttackShape.None });
                        chainRequests.Add(new ChainRequestDescriptor { Shape = MeleeChainAttackShape.None });
                    }
                    else if (doubleChance > 0f && rng.RollPercent(doubleChance))
                    {
                        chainRequests.Add(new ChainRequestDescriptor { Shape = MeleeChainAttackShape.None });
                    }

                    if (flurryMax > 0 && flurryChance > 0f && rng.RollPercent(flurryChance))
                    {
                        for (int i = 0; i < flurryMax; i++)
                        {
                            if (flurryPerAttack <= 0f || !rng.RollPercent(flurryPerAttack))
                                break;
                            chainRequests.Add(new ChainRequestDescriptor { Shape = MeleeChainAttackShape.None });
                        }
                    }

                    var areaShape = stats.MultiAreaShape != MeleeChainAttackShape.None ? stats.MultiAreaShape : config.AreaShape;
                    if (areaShape != MeleeChainAttackShape.None && areaChance > 0f && rng.RollPercent(areaChance))
                    {
                        var descriptor = new ChainRequestDescriptor
                        {
                            Shape = areaShape,
                            ArcDegrees = areaShape == MeleeChainAttackShape.TrueArea ? 0f : (stats.MultiAreaArcDegrees > 0f ? stats.MultiAreaArcDegrees : config.AreaArcDegrees),
                            Radius = stats.MultiAreaRadius > 0f ? stats.MultiAreaRadius : config.AreaRadius,
                            MaxTargets = stats.MultiAreaMaxTargets > 0 ? stats.MultiAreaMaxTargets : config.AreaMaxTargets
                        };
                        chainRequests.Add(descriptor);
                    }

                    if (chainRequests.Length == 0)
                    {
                        context.MultiAttackResolved = 1;
                        context.DeterministicSeed = rng.SerializeState();
                        continue;
                    }

                    for (int i = 0; i < chainRequests.Length; i++)
                    {
                        var descriptor = chainRequests[i];

                        var request = new MeleeAttackRequestElement
                        {
                            Attacker = attacker,
                            WeaponSlot = context.WeaponSlot,
                            AimDirection = context.AimDirection,
                            PreferredTarget = context.PreferredTarget,
                            Flags = MeleeRequestFlags.MultiAttackChain | MeleeRequestFlags.SkipStaminaCost,
                            ChainDepth = nextDepth,
                            ChainShape = descriptor.Shape,
                            ChainArcDegrees = descriptor.ArcDegrees,
                            ChainRadius = descriptor.Radius,
                            ChainMaxTargets = descriptor.MaxTargets,
                            ChainDelaySeconds = chainDelaySeconds,
                            ChainLockoutSeconds = chainLockoutSeconds
                        };

                        request.RequestId = ComposeRequestId(frameToken, (uint)requests.Length);
                        requests.Add(request);
                    }

                    context.MultiAttackResolved = 1;
                    context.DeterministicSeed = rng.SerializeState();
                }
                finally
                {
                    chainRequests.Dispose();
                }
            }
        }

        private struct ChainRequestDescriptor
        {
            public MeleeChainAttackShape Shape;
            public float ArcDegrees;
            public float Radius;
            public int MaxTargets;
        }

        private static uint ComposeRequestId(uint frameCount, uint localIndex)
        {
            return (frameCount << 12) ^ (localIndex * 2654435761u);
        }
    }
}
