using NUnit.Framework;
using Framework.Core.Shared;

namespace Framework.Core.Tests
{
    public class BasePolicyTests
    {
        [Test]
        public void CombatMath_Clamp01_Works()
        {
            Assert.AreEqual(0f, CombatMath.Clamp01(-1f));
            Assert.AreEqual(1f, CombatMath.Clamp01(2f));
            Assert.AreEqual(0.5f, CombatMath.Clamp01(0.5f));
        }
    }
}

