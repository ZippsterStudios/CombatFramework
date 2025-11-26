using Framework.ActionBlock.Runtime;
using Framework.ActionBlock.Requests;
using Unity.Entities;

namespace Framework.ActionBlock.Runtime
{
    public struct ActionBlockSubsystemManifest : Framework.Core.Base.ISubsystemManifest
    {
        public void Register(World world, EntityManager em)
        {
            Framework.Core.Base.SystemRegistration.RegisterISystemInGroups<ActionBlockRuntimeSystem>(world);
        }
    }
}

