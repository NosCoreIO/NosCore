//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.InventoryService
{
    [TestClass]
    public class InventoryServiceLogTests
    {
        private TestWorld World;
        private Mock<ILogger<GameObject.Services.InventoryService.InventoryService>> _logger;
        private GameObject.Services.InventoryService.InventoryService _inventory;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _logger = World.Freeze<Mock<ILogger<GameObject.Services.InventoryService.InventoryService>>>();
            _inventory = World.Create<GameObject.Services.InventoryService.InventoryService>();
        }

        [TestMethod]
        public void ARefusedMoveIsLoggedUnderItsKey()
        {
            new Spec("Moving an item onto its own slot is refused and logged with the key's event id")
                .When(MovingSlot_OntoItself, (short)3)
                .Then(TheLogCarriesTheIdOf_, LogLanguageKey.ITEM_MOVED_ONTO_ITSELF)
                .Execute();
        }

        [TestMethod]
        public void RemovingFromAnEmptySlotIsLoggedUnderItsKey()
        {
            new Spec("Removing from an empty slot is logged with the key's event id")
                .When(RemovingFromEmptySlot_, (short)7)
                .Then(TheLogCarriesTheIdOf_, LogLanguageKey.INVENTORY_SLOT_EMPTY)
                .Execute();
        }

        private void MovingSlot_OntoItself(short slot) =>
            Assert.IsNull(_inventory.MoveInPocket(slot, NoscorePocketType.Equipment, NoscorePocketType.Equipment, slot, false));

        private void RemovingFromEmptySlot_(short slot) =>
            Assert.IsNull(_inventory.DeleteFromTypeAndSlot(NoscorePocketType.Main, slot));

        private void TheLogCarriesTheIdOf_(LogLanguageKey key) =>
            _logger.Verify(l => l.Log(LogLevel.Error, LogEventIds.For(key.ToString()), It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}
