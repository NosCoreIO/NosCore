//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations.Character;
using NosCore.GameObject.Services.MateService;

namespace NosCore.GameObject.Tests.Services.MateService
{
    [TestClass]
    public class MateXpTableTests
    {
        [DataTestMethod]
        [DataRow((byte)1, 15L)]
        [DataRow((byte)3, 90L)]
        [DataRow((byte)4, 165L)]
        [DataRow((byte)5, 273L)]
        [DataRow((byte)6, 420L)]
        [DataRow((byte)14, 3720L)]
        [DataRow((byte)86, 29312950L)]
        [DataRow((byte)88, 39495200L)]
        public void PetRequirementMatchesTheCapture(byte level, long expected)
        {
            Assert.AreEqual(expected, MateXpTable.RequiredXp(level, MateType.Pet),
                $"a level {level} pet asked for a different amount than the captured sc_p reported");
        }

        [DataTestMethod]
        [DataRow((byte)24, 117720L)]
        [DataRow((byte)50, 2293816L)]
        public void PartnerRequirementMatchesTheCapture(byte level, long expected)
        {
            Assert.AreEqual(expected, MateXpTable.RequiredXp(level, MateType.Partner),
                $"a level {level} partner asked for a different amount than the captured sc_n reported");
        }

        [TestMethod]
        public void PartnerNeedsFourTimesWhatAPetNeeds()
        {
            for (byte level = 1; level < 100; level++)
            {
                Assert.AreEqual(MateXpTable.RequiredXp(level, MateType.Pet) * 4,
                    MateXpTable.RequiredXp(level, MateType.Partner),
                    $"the pet/partner ratio broke at level {level}");
            }
        }

        [TestMethod]
        public void RequirementNeverGoesBackwards()
        {
            var previous = 0L;
            for (byte level = 1; level < 100; level++)
            {
                var current = MateXpTable.RequiredXp(level, MateType.Pet);
                Assert.IsTrue(current >= previous,
                    $"level {level} needs less experience than level {level - 1}");
                previous = current;
            }
        }

        [TestMethod]
        public void AskingBeyondTheTableDoesNotThrow()
        {
            Assert.IsTrue(MateXpTable.RequiredXp(byte.MaxValue, MateType.Pet) > 0);
            Assert.IsTrue(MateXpTable.RequiredXp(0, MateType.Pet) > 0);
        }
    }
}
