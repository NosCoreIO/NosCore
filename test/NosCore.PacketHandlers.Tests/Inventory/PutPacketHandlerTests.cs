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
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.PacketHandlers.Inventory;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.PacketHandlers.Tests.Inventory
{
    [TestClass]
    public class PutPacketHandlerTests
    {
        private IItemGenerationService? _item;
        private PutPacketHandler? _putPacketHandler;
        private ClientSession? _session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            _item = TestHelpers.Instance.GenerateItemProvider();
            _session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            _putPacketHandler = new PutPacketHandler(TestHelpers.Instance.WorldConfiguration, TestHelpers.Instance.GameLanguageLocalizer);
        }

        [TestMethod]
        public async Task Test_PutPartialSlotAsync()
        {
            _session!.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(_item!.Create(1012, 999), 0));
            await _putPacketHandler!.ExecuteAsync(new PutPacket
            {
                PocketType = PocketType.Main,
                Slot = 0,
                Amount = 500
            }, _session).ConfigureAwait(false);
            Assert.IsTrue((_session.Character.InventoryService.Count == 1) &&
                (_session.Character.InventoryService.FirstOrDefault().Value.ItemInstance?.Amount == 499));
        }

        [TestMethod]
        public async Task Test_PutNotDroppableAsync()
        {
            _session!.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(_item!.Create(1013, 1), 0));
            await _putPacketHandler!.ExecuteAsync(new PutPacket
            {
                PocketType = PocketType.Main,
                Slot = 0,
                Amount = 1
            }, _session).ConfigureAwait(false);
            var packet = (SayiPacket?)_session.LastPackets.FirstOrDefault(s => s is SayiPacket);
            Assert.IsTrue(packet?.VisualType == VisualType.Player && packet?.VisualId == _session.Character.CharacterId &&
                packet?.Type == SayColorType.Yellow && packet?.Message == Game18NConstString.CantDropItem);
            Assert.IsTrue(_session.Character.InventoryService.Count > 0);
        }


        [TestMethod]
        public async Task Test_PutAsync()
        {
            _session!.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(_item!.Create(1012, 1), 0));
            await _putPacketHandler!.ExecuteAsync(new PutPacket
            {
                PocketType = PocketType.Main,
                Slot = 0,
                Amount = 1
            }, _session).ConfigureAwait(false);
            Assert.IsTrue(_session.Character.InventoryService.Count == 0);
        }

        [TestMethod]
        public async Task Test_PutBadPlaceAsync()
        {
            _session!.Character.PositionX = 2;
            _session.Character.PositionY = 2;
            _session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(_item!.Create(1012, 1), 0));
            await _putPacketHandler!.ExecuteAsync(new PutPacket
            {
                PocketType = PocketType.Main,
                Slot = 0,
                Amount = 1
            }, _session).ConfigureAwait(false);
            var packet = (SayiPacket?)_session.LastPackets.FirstOrDefault(s => s is SayiPacket);
            Assert.IsTrue(packet?.VisualType == VisualType.Player && packet?.VisualId == _session.Character.CharacterId &&
                packet?.Type == SayColorType.Yellow && packet?.Message == Game18NConstString.CantDropItem);
            Assert.IsTrue(_session.Character.InventoryService.Count > 0);
        }

        [TestMethod]
        public async Task Test_PutOutOfBoundsAsync()
        {
            _session!.Character.PositionX = -1;
            _session.Character.PositionY = -1;
            _session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(_item!.Create(1012, 1), 0));
            await _putPacketHandler!.ExecuteAsync(new PutPacket
            {
                PocketType = PocketType.Main,
                Slot = 0,
                Amount = 1
            }, _session).ConfigureAwait(false);
            var packet = (SayiPacket?)_session.LastPackets.FirstOrDefault(s => s is SayiPacket);
            Assert.IsTrue(packet?.VisualType == VisualType.Player && packet?.VisualId == _session.Character.CharacterId &&
                packet?.Type == SayColorType.Yellow && packet?.Message == Game18NConstString.CantDropItem);
            Assert.IsTrue(_session.Character.InventoryService.Count > 0);
        }
    }
}