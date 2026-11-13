//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Parser.Parsers;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class EffectValueTests
    {
        [TestMethod]
        [DataRow(0, 0, false, false)]
        [DataRow(400, 100, false, false)]
        [DataRow(-400, 100, false, false)]
        [DataRow(13, 3, true, false)]
        [DataRow(14, 3, true, true)]
        [DataRow(-3, 1, true, false)]
        [DataRow(-15, 4, true, false)]
        [DataRow(-19, 5, true, false)]
        [DataRow(-31, 8, true, false)]
        [DataRow(-10, 3, true, true)]
        public void TheFlagsAreTheTwoLowBitsOfTheSignedValue(int raw, int value, bool scaled, bool divided)
        {
            var decoded = EffectValue.Decode(raw);

            Assert.AreEqual(value, decoded.Value);
            Assert.AreEqual(scaled, decoded.IsLevelScaled);
            Assert.AreEqual(divided, decoded.IsLevelDivided);
        }
    }
}
