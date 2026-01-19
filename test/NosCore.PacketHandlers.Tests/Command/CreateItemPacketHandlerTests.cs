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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.PacketHandlers.Command;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.I18N;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Command
{
    [TestClass]
    public class CreateItemPacketHandlerTests
    {
        private CreateItemPackettHandler Handler = null!;
        private ClientSession Session = null!;
        private List<ItemDto> Items = null!;
        private Mock<IItemGenerationService> ItemProvider = null!;
        private readonly ILogger Logger = new Mock<ILogger>().Object;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            ItemProvider = new Mock<IItemGenerationService>();
            Items = new List<ItemDto>
            {
                new Item { VNum = 1012, Type = NoscorePocketType.Main },
                new Item { VNum = 1, Type = NoscorePocketType.Equipment, EquipmentSlot = EquipmentType.MainWeapon, ItemType = ItemType.Weapon },
                new Item { VNum = 912, Type = NoscorePocketType.Equipment, EquipmentSlot = EquipmentType.Sp, ItemType = ItemType.Specialist }
            };

            ItemProvider.Setup(x => x.Create(It.IsAny<short>(), It.IsAny<short>(), It.IsAny<sbyte>(), It.IsAny<byte>(), It.IsAny<byte>()))
                .Returns((short vnum, short amount, sbyte rare, byte upgrade, byte design) =>
                    new ItemInstance(new Item { VNum = vnum, Type = NoscorePocketType.Main }) { Amount = amount, Rare = rare, Upgrade = upgrade });

            Handler = new CreateItemPackettHandler(
                Logger,
                Items,
                TestHelpers.Instance.WorldConfiguration,
                ItemProvider.Object,
                TestHelpers.Instance.LogLanguageLocalizer);
        }

        [TestMethod]
        public async Task CreatingNonExistentItemShouldShowError()
        {
            await new Spec("Creating non-existent item should show error")
                .Given(CharacterIsOnMap)
                .WhenAsync(CreatingNonExistentItem)
                .Then(ShouldReceiveItemDoesNotExistMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CreatingGoldItemShouldBeIgnored()
        {
            await new Spec("Creating gold item (VNum 1046) should be ignored")
                .Given(CharacterIsOnMap)
                .WhenAsync(CreatingGoldItem)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CreatingValidItemShouldAddToInventory()
        {
            await new Spec("Creating valid item should add to inventory")
                .Given(CharacterIsOnMap)
                .WhenAsync(CreatingValidItem)
                .Then(ShouldReceiveItemReceivedMessage)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
        }

        private async Task CreatingNonExistentItem()
        {
            await Handler.ExecuteAsync(new CreateItemPacket
            {
                VNum = 9999
            }, Session);
        }

        private async Task CreatingGoldItem()
        {
            Session.LastPackets.Clear();
            await Handler.ExecuteAsync(new CreateItemPacket
            {
                VNum = 1046
            }, Session);
        }

        private async Task CreatingValidItem()
        {
            await Handler.ExecuteAsync(new CreateItemPacket
            {
                VNum = 1012,
                DesignOrAmount = 5
            }, Session);
        }

        private void ShouldReceiveItemDoesNotExistMessage()
        {
            var packet = Session.LastPackets.OfType<MsgiPacket>().FirstOrDefault();
            Assert.IsNotNull(packet);
            Assert.AreEqual(Game18NConstString.ItemDoesNotExist, packet.Message);
        }

        private void NoPacketShouldBeSent()
        {
            Assert.AreEqual(0, Session.LastPackets.Count);
        }

        private void ShouldReceiveItemReceivedMessage()
        {
            var packet = Session.LastPackets.OfType<SayiPacket>().FirstOrDefault();
            Assert.IsNotNull(packet);
            Assert.AreEqual(Game18NConstString.ReceivedThisItem, packet.Message);
        }
    }
}
