using Framework.ActionBlock.Components;
using Framework.ActionBlock.Config;
using Framework.ActionBlock.Policies;
using Framework.ActionBlock.Requests;
using Unity.Collections;
using Unity.Entities;

#if FRAMEWORK_HAS_LIFECYCLE
using Framework.Lifecycle.Config;
#endif

namespace Framework.ActionBlock.Runtime
{
    [UpdateInGroup(typeof(Framework.Core.Base.RuntimeSystemGroup))]
    public partial struct ActionBlockRuntimeSystem : ISystem
    {
        private EntityQuery _requestQuery;

        public void OnCreate(ref SystemState state)
        {
            _requestQuery = state.GetEntityQuery(ComponentType.ReadOnly<ActionBlockRequest>());
        }

        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state)
        {
            if (TryResolveConfig(ref state, out var resolved))
                ActionBlockConfigAccess.UpdateConfig(resolved);
            else
                ActionBlockConfigAccess.Reset();

            if (_requestQuery.IsEmptyIgnoreFilter)
                return;

            var em = state.EntityManager;
            em.CompleteDependencyBeforeRW<ActionBlockMask>();

            using var entities = _requestQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                var source = entities[i];
                var buffer = em.GetBuffer<ActionBlockRequest>(source);
                for (int r = 0; r < buffer.Length; r++)
                {
                    var request = buffer[r];
                    if (!em.Exists(request.Target))
                        continue;

                    if (!em.HasComponent<ActionBlockMask>(request.Target))
                        em.AddComponentData(request.Target, new ActionBlockMask());

                    var mask = em.GetComponentData<ActionBlockMask>(request.Target);
                    if (request.Add)
                        ActionBits.Set(ref mask, request.Kind);
                    else
                        ActionBits.Clear(ref mask, request.Kind);

                    em.SetComponentData(request.Target, mask);
                }
                buffer.Clear();
            }
        }

        private bool TryResolveConfig(ref SystemState state, out ActionBlockConfig config)
        {
            if (SystemAPI.TryGetSingleton(out ActionBlockConfig explicitConfig))
            {
                config = explicitConfig;
                return true;
            }

#if FRAMEWORK_HAS_LIFECYCLE
            if (SystemAPI.TryGetSingleton(out LifecycleFeatureConfig lifecycle))
            {
                config = new ActionBlockConfig
                {
                    BlocksRespectDead = lifecycle.BlocksRespectDead,
                    BlocksRespectCrowdControl = lifecycle.BlocksRespectCrowdControl,
                    BlocksRespectCustomRules = lifecycle.BlocksRespectCustomRules
                };
                return true;
            }
#endif

            config = ActionBlockConfig.Default;
            return false;
        }
    }
}
