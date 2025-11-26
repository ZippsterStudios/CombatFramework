using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Entities;

namespace Framework.Core.Base
{
    public static class SubsystemBootstrap
    {
        private static readonly string[] FallbackManifestTypes =
        {
            "Framework.Buffs.Runtime.BuffSubsystemManifest, Framework.Buffs",
            "Framework.Debuffs.Runtime.DebuffSubsystemManifest, Framework.Debuffs",
            "Framework.Cooldowns.Runtime.CooldownSubsystemManifest, Framework.Cooldowns",
            "Framework.Damage.Runtime.DamageSubsystemManifest, Framework.Damage",
            "Framework.Heal.Runtime.HealSubsystemManifest, Framework.Heal",
            "Framework.DOT.Runtime.DotSubsystemManifest, Framework.DOT",
            "Framework.HOT.Runtime.HotSubsystemManifest, Framework.HOT",
            "Framework.Resources.Runtime.ResourceSubsystemManifest, Framework.Resources",
            "Framework.Melee.Runtime.MeleeSubsystemManifest, Framework.Melee",
            "Framework.Spells.Runtime.SpellSubsystemManifest, Framework.Spells",
            "Framework.Stats.Runtime.StatSubsystemManifest, Framework.Stats",
            "Framework.Threat.Runtime.ThreatSubsystemManifest, Framework.Threat",
            "Framework.Temporal.Runtime.TemporalSubsystemManifest, Framework.Temporal",
            "Framework.AreaEffects.Runtime.AreaEffectSubsystemManifest, Framework.AreaEffects",
            "Framework.Pets.Runtime.PetSubsystemManifest, Framework.Pets",
            "Framework.AI.Runtime.AISubsystemManifest, Framework.AI",
            "Framework.DamageModifiers.Runtime.DamageModifierSubsystemManifest, Framework.DamageModifiers",
            "Framework.Lifecycle.Runtime.LifecycleSubsystemManifest, Framework.Lifecycle",
            "Framework.ActionBlock.Runtime.ActionBlockSubsystemManifest, Framework.ActionBlock",
            "Framework.Shadow.Runtime.ShadowSubsystemManifest, Framework.Shadow"
        };

        public static void InstallAll(World world)
        {
            var em = world.EntityManager;
            if (TryInstallFromAggregator(world, em))
                return;

            InstallFallbackManifests(world, em);
        }

        private static bool TryInstallFromAggregator(World world, EntityManager em)
        {
            const string typeName = "Framework.CombatSystem.Bootstrap.SubsystemManifestRegistry";
            Type registryType = null;
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length && registryType == null; i++)
            {
                registryType = assemblies[i].GetType(typeName, throwOnError: false);
            }
            if (registryType == null)
                return false;

            var build = registryType.GetMethod("Build", BindingFlags.Public | BindingFlags.Static);
            if (build == null)
                return false;

            build.Invoke(null, new object[] { world, em });
            return true;
        }

        private static void InstallFallbackManifests(World world, EntityManager em)
        {
            var manifests = new List<ISubsystemManifest>(FallbackManifestTypes.Length);
            for (int i = 0; i < FallbackManifestTypes.Length; i++)
            {
                var qualifiedName = FallbackManifestTypes[i];
                var type = Type.GetType(qualifiedName);
                if (type == null)
                    continue;
                if (!typeof(ISubsystemManifest).IsAssignableFrom(type))
                    continue;
                if (Activator.CreateInstance(type) is ISubsystemManifest manifest)
                    manifests.Add(manifest);
            }

            for (int i = 0; i < manifests.Count; i++)
            {
                manifests[i].Register(world, em);
            }
        }
    }
}
