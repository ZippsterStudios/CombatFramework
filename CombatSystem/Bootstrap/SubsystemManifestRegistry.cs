using System;
using System.Collections.Generic;
using Framework.Core.Base;
using Unity.Entities;

namespace Framework.CombatSystem.Bootstrap
{
    public static class SubsystemManifestRegistry
    {
        private static readonly List<ISubsystemManifest> _manifests = new List<ISubsystemManifest>(32);
        private static bool _defaultsRegistered;

        public static void RegisterSubsystem<T>() where T : struct, ISubsystemManifest
        {
            _manifests.Add(new T());
        }

        private static void RegisterAllDefaults()
        {
            if (_defaultsRegistered) return;
            _defaultsRegistered = true;

            // Core gameplay subsystems
            RegisterSubsystem<Framework.Buffs.Runtime.BuffSubsystemManifest>();
            RegisterSubsystem<Framework.Debuffs.Runtime.DebuffSubsystemManifest>();
            RegisterSubsystem<Framework.Cooldowns.Runtime.CooldownSubsystemManifest>();
            RegisterSubsystem<Framework.Damage.Runtime.DamageSubsystemManifest>();
            RegisterSubsystem<Framework.Heal.Runtime.HealSubsystemManifest>();
            RegisterSubsystem<Framework.DOT.Runtime.DotSubsystemManifest>();
            RegisterSubsystem<Framework.HOT.Runtime.HotSubsystemManifest>();
            RegisterSubsystem<Framework.Resources.Runtime.ResourceSubsystemManifest>();
            RegisterSubsystem<Framework.Melee.Runtime.MeleeSubsystemManifest>();
            RegisterSubsystem<Framework.Spells.Runtime.SpellSubsystemManifest>();
            RegisterSubsystem<Framework.Stats.Runtime.StatSubsystemManifest>();
            RegisterSubsystem<Framework.Threat.Runtime.ThreatSubsystemManifest>();
            RegisterSubsystem<Framework.Temporal.Runtime.TemporalSubsystemManifest>();
            RegisterSubsystem<Framework.AreaEffects.Runtime.AreaEffectSubsystemManifest>();
            RegisterSubsystem<Framework.Pets.Runtime.PetSubsystemManifest>();
            // AI runtime is included for completeness
            RegisterSubsystem<Framework.AI.Runtime.AISubsystemManifest>();

            TryRegisterSubsystem("Framework.DamageModifiers.Runtime.DamageModifierSubsystemManifest, Framework.DamageModifiers");
            TryRegisterSubsystem("Framework.Lifecycle.Runtime.LifecycleSubsystemManifest, Framework.Lifecycle");
            TryRegisterSubsystem("Framework.ActionBlock.Runtime.ActionBlockSubsystemManifest, Framework.ActionBlock");
        }

        public static void Build(World world, EntityManager em)
        {
            RegisterAllDefaults();
            for (int i = 0; i < _manifests.Count; i++)
            {
                _manifests[i].Register(world, em);
            }
        }

        private static void TryRegisterSubsystem(string qualifiedName)
        {
            var type = Type.GetType(qualifiedName);
            if (type == null)
                return;

            if (!typeof(ISubsystemManifest).IsAssignableFrom(type))
                return;

            if (Activator.CreateInstance(type) is ISubsystemManifest manifest)
                _manifests.Add(manifest);
        }
    }
}
