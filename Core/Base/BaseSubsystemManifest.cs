using Unity.Entities;

namespace Framework.Core.Base
{
    public interface ISubsystemManifest
    {
        void Register(World world, EntityManager em);
    }
}

