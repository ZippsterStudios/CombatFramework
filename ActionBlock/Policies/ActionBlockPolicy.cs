// Define FRAMEWORK_HAS_DEBUFFS to enable crowd-control integrations.
using System;
using Framework.ActionBlock.Components;
using Unity.Burst;
using Unity.Entities;

namespace Framework.ActionBlock.Policies
{
    public static class ActionBlockPolicy
    {
        public static bool Can(in EntityManager em, in Entity entity, ActionKind kind)
        {
            if (!em.Exists(entity))
                return false;

            var config = ActionBlockConfigAccess.Get();

            if (config.BlocksRespectDead && DeadLookup.TryHasComponent(em, entity))
                return false;

#if FRAMEWORK_HAS_DEBUFFS
            if (config.BlocksRespectCrowdControl && em.HasComponent<Framework.Debuffs.Components.DebuffCrowdControlState>(entity))
            {
                var cc = em.GetComponentData<Framework.Debuffs.Components.DebuffCrowdControlState>(entity);
                if (MapCrowdControlToBlocks(cc.ActiveFlags, kind))
                    return false;
            }
#endif

            if (config.BlocksRespectCustomRules && em.HasComponent<ActionBlockMask>(entity))
            {
                var mask = em.GetComponentData<ActionBlockMask>(entity);
                if (ActionBits.IsBlocked(in mask, kind))
                    return false;
            }

            return true;
        }

#if FRAMEWORK_HAS_DEBUFFS
        public static bool MapCrowdControlToBlocks(Framework.Debuffs.Content.DebuffFlags flags, ActionKind kind)
        {
            if ((flags & Framework.Debuffs.Content.DebuffFlags.Stun) != 0)
                return true;

            switch (kind)
            {
                case ActionKind.Move:
                    return (flags & Framework.Debuffs.Content.DebuffFlags.Root) != 0 ||
                           (flags & Framework.Debuffs.Content.DebuffFlags.Fear) != 0 ||
                           (flags & Framework.Debuffs.Content.DebuffFlags.Mez) != 0;
                case ActionKind.Attack:
                    return (flags & Framework.Debuffs.Content.DebuffFlags.Mez) != 0 ||
                           (flags & Framework.Debuffs.Content.DebuffFlags.Disarm) != 0 ||
                           (flags & Framework.Debuffs.Content.DebuffFlags.Fear) != 0;
                case ActionKind.Cast:
                    return (flags & Framework.Debuffs.Content.DebuffFlags.Silence) != 0 ||
                           (flags & Framework.Debuffs.Content.DebuffFlags.Mez) != 0 ||
                           (flags & Framework.Debuffs.Content.DebuffFlags.Fear) != 0;
                case ActionKind.Interact:
                case ActionKind.UseItem:
                    return (flags & (Framework.Debuffs.Content.DebuffFlags.Stun | Framework.Debuffs.Content.DebuffFlags.Mez | Framework.Debuffs.Content.DebuffFlags.Fear)) != 0;
                default:
                    return false;
            }
        }
#endif
    }

    internal static class DeadLookup
    {
        private struct TypeCacheTag { }
        private struct StateTag { }

        private static readonly SharedStatic<ComponentType> _type = SharedStatic<ComponentType>.GetOrCreate<TypeCacheTag>();
        private static readonly SharedStatic<byte> _state = SharedStatic<byte>.GetOrCreate<StateTag>();

        public static bool TryHasComponent(in EntityManager em, in Entity entity)
        {
            if (_state.Data == 0)
            {
                var resolved = Type.GetType("Framework.Lifecycle.Components.Dead, Framework.Lifecycle");
                if (resolved != null)
                {
                    _type.Data = ComponentType.ReadOnly(resolved);
                    _state.Data = 1;
                }
                else
                {
                    _state.Data = 2;
                }
            }

            if (_state.Data != 1)
                return false;

            return em.HasComponent(entity, _type.Data);
        }
    }
}
