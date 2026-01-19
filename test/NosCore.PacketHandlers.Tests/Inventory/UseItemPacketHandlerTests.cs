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
using NosCore.Data.Enumerations;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.PacketHandlers.Inventory;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Inventory
{
    [TestClass]
    public class UseItemPacketHandlerTests
    {
        private IItemGenerationService Item = null!;
        private ClientSession Session = null!;
        private UseItemPacketHandler UseItemPacketHandler = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Item = TestHelpers.Instance.GenerateItemProvider();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            UseItemPacketHandler = new UseItemPacketHandler();
        }

        [TestMethod]
        public async Task UsingItemShouldBindToCharacter()
        {
            await new Spec("Using item should bind to character")
                .Given(CharacterHasEquipmentItem)
                .WhenAsync(UsingItemWithBinding)
                .Then(ItemShouldBeWornAndBound)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UsingSpScrollShouldIncrementAdditionPoints()
        {
            await new Spec("Using sp scroll should increment addition points")
                .Given(CharacterHasZeroSpAdditionPoints)
                .And(CharacterHasSpScroll)
                .WhenAsync(UsingSpScroll)
                .Then(SpAdditionPointsShouldIncrease)
                .And(NoErrorMessageShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UsingSpScrollAtMaxShouldShowError()
        {
            await new Spec("Using sp scroll at max should show error")
                .Given(CharacterHasMaxSpAdditionPoints)
                .And(CharacterHasSpScroll)
                .WhenAsync(UsingSpScroll)
                .Then(ShouldReceiveCapacityExceededMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UsingSpScrollCloseToLimitShouldCapAtMax()
        {
            await new Spec("Using sp scroll close to limit should cap at max")
                .Given(CharacterHasAlmostMaxSpAdditionPoints)
                .And(CharacterHasSpScroll)
                .WhenAsync(UsingSpScroll)
                .Then(SpAdditionPointsShouldBeAtMax)
                .And(NoErrorMessageShouldBeSent)
                .ExecuteAsync();
        }

        private void CharacterHasEquipmentItem()
        {
            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(Item.Create(1, 1), 0));
        }

        private void CharacterHasZeroSpAdditionPoints()
        {
            Session.Character.SpAdditionPoint = 0;
        }

        private void CharacterHasSpScroll()
        {
            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(Item.Create(1078, 1), 0));
        }

        private void CharacterHasMaxSpAdditionPoints()
        {
            Session.Character.SpAdditionPoint = TestHelpers.Instance.WorldConfiguration.Value.MaxAdditionalSpPoints;
        }

        private void CharacterHasAlmostMaxSpAdditionPoints()
        {
            Session.Character.SpAdditionPoint = TestHelpers.Instance.WorldConfiguration.Value.MaxAdditionalSpPoints - 1;
        }

        private async Task UsingItemWithBinding()
        {
            await UseItemPacketHandler.ExecuteAsync(new UseItemPacket { Slot = 0, Type = PocketType.Equipment, Mode = 1 }, Session);
        }

        private async Task UsingSpScroll()
        {
            var item = Session.Character.InventoryService.First();
            await UseItemPacketHandler.ExecuteAsync(new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = 1,
                Type = (PocketType)item.Value.Type,
                Slot = item.Value.Slot,
                Mode = 0,
                Parameter = 0
            }, Session);
        }

        private void ItemShouldBeWornAndBound()
        {
            Assert.IsTrue(Session.Character.InventoryService.Any(s =>
                s.Value.ItemInstance.ItemVNum == 1 &&
                s.Value.Type == NoscorePocketType.Wear &&
                s.Value.ItemInstance.BoundCharacterId == Session.Character.VisualId));
        }

        private void SpAdditionPointsShouldIncrease()
        {
            Assert.IsTrue(Session.Character.SpAdditionPoint != 0);
        }

        private void SpAdditionPointsShouldBeAtMax()
        {
            Assert.AreEqual(TestHelpers.Instance.WorldConfiguration.Value.MaxAdditionalSpPoints, Session.Character.SpAdditionPoint);
        }

        private void NoErrorMessageShouldBeSent()
        {
            Assert.IsFalse(Session.LastPackets.Any(s => s is MsgiPacket));
        }

        private void ShouldReceiveCapacityExceededMessage()
        {
            var packet = (MsgiPacket?)Session.LastPackets.FirstOrDefault(s => s is MsgiPacket);
            Assert.IsTrue(packet?.Type == MessageType.Default && packet?.Message == Game18NConstString.CannotBeUsedExceedsCapacity);
        }
    }
}
