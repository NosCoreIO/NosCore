//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Dto;

namespace NosCore.GameObject.Tests.Maps
{
    [TestClass]
    public class MapTests
    {
        [TestMethod]
        [DataRow((short)3, (short)3, true)]
        [DataRow((short)4, (short)3, false)]
        [DataRow((short)3, (short)4, false)]
        [DataRow((short)4, (short)4, false)]
        [DataRow((short)-1, (short)0, false)]
        [DataRow((short)0, (short)-1, false)]
        public void IsWalkableTreatsCellsOutsideTheGridAsBlocked(short x, short y, bool expected)
        {
            var map = new GameObject.Map.Map
            {
                Data = new byte[4 + 4 * 4] { 4, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                Name = new I18NString(),
            };

            Assert.AreEqual(expected, map.IsWalkable(x, y));
        }
    }
}
