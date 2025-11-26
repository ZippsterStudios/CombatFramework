using NUnit.Framework;
using Unity.Mathematics;

namespace Framework.CombatSystem.Tests
{
    public class AreaEffectTests
    {
        [Test]
        public void CircleContains_Works()
        {
            var c = new float2(0, 0);
            Assert.IsTrue(Framework.AreaEffects.Spatial.Utilities.Overlap.CircleContains(c, 5f, new float2(3, 4)));
            Assert.IsFalse(Framework.AreaEffects.Spatial.Utilities.Overlap.CircleContains(c, 5f, new float2(4, 4)));
        }
    }
}

