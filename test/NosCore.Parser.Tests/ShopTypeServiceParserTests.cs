//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Parser.Parsers;
using System.Linq;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class ShopTypeServiceParserTests
    {
        private const string ShopTypeDat = "#Data format\r# index service...(up to 6)\r\r#4 upgrade\r#6 rarify\r\r#Blacksmith\r3 1 2 3 4 5\r\r#Alchemist\r7 11 12 13 20 45 94\r\r#Family manager\r48 41\r\r#Pyjama\r112 7 \r~\r";

        [TestMethod]
        public void EachShopTypeGetsTheServicesNosCoreHandles()
        {
            var rows = ShopTypeServiceParser.Parse(ShopTypeDat);

            CollectionAssert.AreEquivalent(
                new[]
                {
                    ((byte)3, NpcService.EquipmentUpgrade),
                    ((byte)7, NpcService.ResistanceSum),
                    ((byte)7, NpcService.SpecialistCardUpgrade),
                    ((byte)7, NpcService.SpecialistCardPerfection),
                    ((byte)48, NpcService.FamilyManager),
                },
                rows.Select(r => (r.ShopType, r.Service)).ToArray());
        }

        [TestMethod]
        public void EveryRowGetsItsOwnId()
        {
            var rows = ShopTypeServiceParser.Parse(ShopTypeDat);

            CollectionAssert.AllItemsAreUnique(rows.Select(r => r.ShopTypeServiceId).ToList());
        }
    }
}
