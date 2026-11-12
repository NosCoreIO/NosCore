//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.ItemGenerationService;

namespace NosCore.GameObject.Tests.Services.ItemCatalogTests
{
    [TestClass]
    public class ItemCatalogTests
    {
        private static ItemCatalog Catalog() => new(new List<ItemDto>
        {
            new() { VNum = 1, Effect = ItemEffectType.NoEffect },
            new() { VNum = 1046, Effect = ItemEffectType.Gold },
            new() { VNum = 9039, Effect = ItemEffectType.SoulRevival },
            new() { VNum = 1365, Effect = ItemEffectType.SoulRevival },
        });

        [TestMethod]
        public void TheItemCarryingAnEffectIsFound() =>
            Assert.AreEqual((short)1046, Catalog().GetItemWithEffect(ItemEffectType.Gold)?.VNum);

        [TestMethod]
        public void TheLowestVnumWinsWhenItemsShareAnEffect() =>
            Assert.AreEqual((short)1365, Catalog().GetItemWithEffect(ItemEffectType.SoulRevival)?.VNum);

        [TestMethod]
        public void AnEffectNoItemCarriesFindsNothing() =>
            Assert.IsNull(Catalog().GetItemWithEffect(ItemEffectType.CuarryBankSavingsBook));

        [TestMethod]
        public void AnItemIsFoundByItsVnum() =>
            Assert.AreEqual(ItemEffectType.Gold, Catalog().GetItem(1046)?.Effect);
    }
}
