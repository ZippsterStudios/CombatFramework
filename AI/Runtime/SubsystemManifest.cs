using Framework.Core.Base;
using Unity.Entities;

namespace Framework.AI.Runtime
{
    public struct AISubsystemManifest : ISubsystemManifest
    {
        public void Register(World world, EntityManager em)
        {
            SystemRegistration.RegisterISystemInGroups<AIDecisionSystem>(world);
            SystemRegistration.RegisterISystemInGroups<AIRuntimeSystem>(world);
            SystemRegistration.RegisterISystemInGroups<Framework.AI.Behaviors.Runtime.AIBehaviorRecipeSystem>(world);
            SystemRegistration.RegisterISystemInGroups<AIStateMachineSystem>(world);
        }
    }
}
