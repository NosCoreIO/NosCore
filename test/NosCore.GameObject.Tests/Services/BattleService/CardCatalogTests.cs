//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.BattleService;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class CardCatalogTests
    {
        private const short BoosterVNum = 5119;
        private const short BoostCardId = 336;

        private static CardCatalog Create(ItemEffectType effect) => new(
            [new CardDto { CardId = BoostCardId }],
            [new BCardDto { ItemVNum = BoosterVNum, Type = (byte)BCardType.CardType.Buff, SubType = 11, FirstData = 100, SecondData = BoostCardId }],
            [new ItemDto { VNum = BoosterVNum, Effect = effect }]);

        [TestMethod]
        public void AnItemGrantsTheCardItsBuffNames()
        {
            Assert.AreEqual(BoostCardId, Create(ItemEffectType.SpeedBooster).GetItemCard(BoosterVNum)?.CardId);
        }

        [TestMethod]
        public void AnItemWithoutABuffGrantsNothing()
        {
            Assert.IsNull(Create(ItemEffectType.SpeedBooster).GetItemCard(1));
        }

        [TestMethod]
        public void TheCardIsKnownByTheEffectOfTheItemGrantingIt()
        {
            var catalog = Create(ItemEffectType.SpeedBooster);

            Assert.IsTrue(catalog.IsGrantedBy(BoostCardId, ItemEffectType.SpeedBooster));
            Assert.IsFalse(catalog.IsGrantedBy(BoostCardId, ItemEffectType.HeatResistance));
        }

        [TestMethod]
        public void TheCardForASituationIsFoundByItsTrigger()
        {
            var catalog = new CardCatalog([new CardDto { CardId = 169, Trigger = CardTrigger.ArenaRevival }, new CardDto { CardId = 1 }], [], []);

            Assert.AreEqual((short)169, catalog.GetTriggeredCard(CardTrigger.ArenaRevival)?.CardId);
            Assert.IsNull(catalog.GetTriggeredCard(CardTrigger.SpouseInGroup));
        }
    }
}
