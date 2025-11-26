using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Framework.Core.Base
{
    public static class SystemRegistration
    {
        // Registers an unmanaged ISystem into its declared UpdateInGroup(s) using
        // DefaultWorldInitialization helpers (works across Entities versions).
        public static void RegisterISystemInGroups<T>(World world) where T : unmanaged, ISystem
        {
            var types = new List<Type>(1) { typeof(T) };
            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, types);
        }

        // Registers a managed ComponentSystemBase (e.g., ComponentSystemGroup) into its declared UpdateInGroup(s).
        public static void RegisterManagedSystemInGroups<T>(World world) where T : ComponentSystemBase
        {
            var types = new List<Type>(1) { typeof(T) };
            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, types);
        }
    }
}
