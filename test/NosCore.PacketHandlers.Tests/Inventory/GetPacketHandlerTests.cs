//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
// Copyright (C) 2019 - NosCore
//
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.PacketHandlers.Inventory;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Inventory
{
    [TestClass]
    public class GetPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private GetPacketHandler GetPacketHandler = null!;
        private IItemGenerationService Item = null!;
        private ClientSession Session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Item = TestHelpers.Instance.GenerateItemProvider();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            GetPacketHandler = new GetPacketHandler(Logger, TestHelpers.Instance.DistanceCalculator, TestHelpers.Instance.Clock, TestHelpers.Instance.LogLanguageLocalizer);
        }

        [TestMethod]
        public async Task PickingUpItemShouldAddToInventory()
        {
            await new Spec("Picking up item should add to inventory")
                .Given(CharacterIsAtOrigin)
                .And(MapHasDroppableItem)
                .WhenAsync(PickingUpItem)
                .Then(InventoryShouldHaveItems)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PickingUpItemShouldStackWithExisting()
        {
            await new Spec("Picking up item should stack with existing")
                .Given(CharacterIsAtOrigin)
                .And(MapHasDroppableItem)
                .And(CharacterHasSameItemInInventory)
                .WhenAsync(PickingUpItem)
                .Then(ItemAmountShouldBeTwo)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PickingUpItemWithFullInventoryShouldFail()
        {
            await new Spec("Picking up item with full inventory should fail")
                .Given(CharacterIsAtOrigin)
                .And(MapHasEquipmentItem)
                .And(CharacterHasFullEquipmentInventory)
                .WhenAsync(PickingUpItem)
                .Then(ShouldReceiveNotEnoughSpaceMessage)
                .And(InventoryShouldHaveTwoItems)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PickingUpItemShouldKeepRarity()
        {
            await new Spec("Picking up item should keep rarity")
                .Given(CharacterIsAtOrigin)
                .And(MapHasRareItem)
                .WhenAsync(PickingUpItem)
                .Then(ItemRarityShouldBeSix)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PickingUpSomeoneElsesItemShouldFail()
        {
            await new Spec("Picking up someone elses item should fail")
                .Given(CharacterIsAtOrigin)
                .And(MapHasItemOwnedByAnotherPlayer)
                .WhenAsync(PickingUpItem)
                .Then(ShouldReceiveUnableToPickUpMessage)
                .And(InventoryShouldBeEmpty)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PickingUpSomeoneElsesItemAfterDelayShouldSucceed()
        {
            await new Spec("Picking up someone elses item after delay should succeed")
                .Given(CharacterIsAtOrigin)
                .And(MapHasOldItemOwnedByAnotherPlayer)
                .WhenAsync(PickingUpItem)
                .Then(InventoryShouldHaveItems)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PickingUpItemFromFarAwayShouldFail()
        {
            await new Spec("Picking up item from far away should fail")
                .Given(CharacterIsFarFromItem)
                .And(MapHasDroppableItem)
                .WhenAsync(PickingUpItem)
                .Then(InventoryShouldBeEmpty)
                .ExecuteAsync();
        }

        private void CharacterIsAtOrigin()
        {
            Session.Character.PositionX = 0;
            Session.Character.PositionY = 0;
        }

        private void CharacterIsFarFromItem()
        {
            Session.Character.PositionX = 7;
            Session.Character.PositionY = 7;
        }

        private void MapHasDroppableItem()
        {
            Session.Character.MapInstance.MapItems.TryAdd(100001,
                TestHelpers.Instance.MapItemProvider!.Create(Session.Character.MapInstance, Item.Create(1012, 1), 1, 1));
        }

        private void MapHasEquipmentItem()
        {
            Session.Character.MapInstance.MapItems.TryAdd(100001,
                TestHelpers.Instance.MapItemProvider!.Create(Session.Character.MapInstance, Item.Create(1, 1), 1, 1));
        }

        private void CharacterHasSameItemInInventory()
        {
            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(Item.Create(1012, 1), 0));
        }

        private void CharacterHasFullEquipmentInventory()
        {
            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(Item.Create(1, 1), 0));
            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(Item.Create(1, 1), 0));
        }

        private void MapHasRareItem()
        {
            Session.Character.MapInstance.MapItems.TryAdd(100001,
                TestHelpers.Instance.MapItemProvider!.Create(Session.Character.MapInstance, Item.Create(1, 1, 6), 1, 1));
        }

        private void MapHasItemOwnedByAnotherPlayer()
        {
            var mapItem = TestHelpers.Instance.MapItemProvider!.Create(Session.Character.MapInstance, Item.Create(1012, 1), 1, 1);
            mapItem.VisualId = 1012;
            mapItem.OwnerId = 2;
            mapItem.DroppedAt = TestHelpers.Instance.Clock.GetCurrentInstant();
            Session.Character.MapInstance.MapItems.TryAdd(100001, mapItem);
        }

        private void MapHasOldItemOwnedByAnotherPlayer()
        {
            var mapItem = TestHelpers.Instance.MapItemProvider!.Create(Session.Character.MapInstance, Item.Create(1012, 1), 1, 1);
            mapItem.VisualId = 1012;
            mapItem.OwnerId = 2;
            mapItem.DroppedAt = TestHelpers.Instance.Clock.GetCurrentInstant().Plus(Duration.FromSeconds(-30));
            Session.Character.MapInstance.MapItems.TryAdd(100001, mapItem);
        }

        private async Task PickingUpItem()
        {
            await GetPacketHandler.ExecuteAsync(new GetPacket
            {
                PickerId = Session.Character.CharacterId,
                VisualId = 100001,
                PickerType = VisualType.Player
            }, Session);
        }

        private void InventoryShouldHaveItems()
        {
            Assert.IsTrue(Session.Character.InventoryService.Count > 0);
        }

        private void InventoryShouldBeEmpty()
        {
            Assert.AreEqual(0, Session.Character.InventoryService.Count);
        }

        private void InventoryShouldHaveTwoItems()
        {
            Assert.AreEqual(2, Session.Character.InventoryService.Count);
        }

        private void ItemAmountShouldBeTwo()
        {
            Assert.AreEqual(2, Session.Character.InventoryService.First().Value.ItemInstance.Amount);
        }

        private void ItemRarityShouldBeSix()
        {
            Assert.AreEqual(6, Session.Character.InventoryService.First().Value.ItemInstance.Rare);
        }

        private void ShouldReceiveNotEnoughSpaceMessage()
        {
            var packet = (MsgiPacket?)Session.LastPackets.FirstOrDefault(s => s is MsgiPacket);
            Assert.IsTrue(packet?.Message == Game18NConstString.NotEnoughSpace && packet.Type == 0);
        }

        private void ShouldReceiveUnableToPickUpMessage()
        {
            var packet = (SayiPacket?)Session.LastPackets.FirstOrDefault(s => s is SayiPacket);
            Assert.IsTrue(packet?.VisualType == VisualType.Player &&
                packet?.VisualId == Session.Character.CharacterId &&
                packet?.Type == SayColorType.Yellow &&
                packet?.Message == Game18NConstString.UnableToPickUp);
        }
    }
}
