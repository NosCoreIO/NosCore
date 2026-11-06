//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Algorithm.SpecialistPointService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using CardService = NosCore.GameObject.Services.SpecialistCardService.SpecialistCardService;

namespace NosCore.GameObject.Tests.Services.SpecialistCard
{
    [TestClass]
    public class SpecialistCardServiceTests
    {
        private readonly SpecialistPointService _points = new();
        private CardService _service = null!;

        [TestInitialize]
        public void Setup() => _service = new CardService(_points);

        private static SpecialistInstance Card(byte upgrade) =>
            new(new Item { VNum = 901 }) { SpLevel = 99, Upgrade = upgrade };

        [TestMethod]
        public void AMaximisedPlusFifteenCardStopsAtFourHundredTenPoints()
        {
            var card = Card(15);

            Assert.AreEqual(410, _service.GetFreePoints(card));
            Assert.IsFalse(_service.TrySpendPoints(card, 411, 0, 0, 0));
        }

        [TestMethod]
        public void APlusTwentyCardSpendsPointsPastFourHundredTen()
        {
            var card = Card(20);

            Assert.IsTrue(_service.TrySpendPoints(card, 486, 0, 0, 0));
            Assert.AreEqual(120, _points.GetAttackLevel(card.SlDamage));
        }
    }
}
