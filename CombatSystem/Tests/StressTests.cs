using NUnit.Framework;
using Unity.Entities;
using Framework.CombatSystem.Bootstrap;

namespace Framework.CombatSystem.Tests
{
    public class StressTests
    {
        [Test]
        public void CreateAndDestroyWorld_Repeatedly()
        {
            for (int i = 0; i < 5; i++)
            {
                using var world = new World($"StressWorld_{i}");
                CombatWorldBootstrap.Initialize(world);
            }
            Assert.Pass();
        }
    }
}

