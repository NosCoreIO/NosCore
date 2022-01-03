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
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
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

namespace NosCore.PacketHandlers.Tests.Inventory
{
    [TestClass]
    public class GetPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private GetPacketHandler? _getPacketHandler;
        private IItemGenerationService? _item;
        private ClientSession? _session;
        
        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            _item = TestHelpers.Instance.GenerateItemProvider();
            _session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            _getPacketHandler = new GetPacketHandler(Logger, TestHelpers.Instance.DistanceCalculator, TestHelpers.Instance.Clock);
        }


        [TestMethod]
        public async Task Test_GetAsync()
        {
            _session!.Character.PositionX = 0;
            _session.Character.PositionY = 0;
            _session.Character.MapInstance.MapItems.TryAdd(100001,
                TestHelpers.Instance.MapItemProvider!.Create(_session.Character.MapInstance, _item!.Create(1012, 1), 1,
                    1));

            await _getPacketHandler!.ExecuteAsync(new GetPacket
            {
                PickerId = _session.Character.CharacterId,
                VisualId = 100001,
                PickerType = VisualType.Player
            }, _session).ConfigureAwait(false);
            Assert.IsTrue(_session.Character.InventoryService!.Count > 0);
        }

        [TestMethod]
        public async Task Test_GetInStackAsync()
        {
            _session!.Character.PositionX = 0;
            _session.Character.PositionY = 0;

            _session.Character.MapInstance.MapItems.TryAdd(100001,
                TestHelpers.Instance.MapItemProvider!.Create(_session.Character.MapInstance, _item!.Create(1012, 1), 1,
                    1));
            _session.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1012, 1), 0));
            await _getPacketHandler!.ExecuteAsync(new GetPacket
            {
                PickerId = _session.Character.CharacterId,
                VisualId = 100001,
                PickerType = VisualType.Player
            }, _session).ConfigureAwait(false);
            Assert.IsTrue(_session.Character.InventoryService.First().Value.ItemInstance!.Amount == 2);
        }

        [TestMethod]
        public async Task Test_GetFullInventoryAsync()
        {
            _session!.Character.PositionX = 0;
            _session.Character.PositionY = 0;
            _session.Character.MapInstance.MapItems.TryAdd(100001,
                TestHelpers.Instance.MapItemProvider!.Create(_session.Character.MapInstance, _item!.Create(1, 1), 1, 1));
            _session.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1), 0));
            _session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1), 0));
            await _getPacketHandler!.ExecuteAsync(new GetPacket
            {
                PickerId = _session.Character.CharacterId,
                VisualId = 100001,
                PickerType = VisualType.Player
            }, _session).ConfigureAwait(false);
            var packet = (MsgiPacket?)_session.LastPackets.FirstOrDefault(s => s is MsgiPacket);
            Assert.IsTrue(packet?.Message == Game18NConstString.NotEnoughSpace && packet.Type == 0);
            Assert.IsTrue(_session.Character.InventoryService.Count == 2);
        }

        [TestMethod]
        public async Task Test_Get_KeepRarityAsync()
        {
            _session!.Character.PositionX = 0;
            _session.Character.PositionY = 0;
            _session.Character.MapInstance.MapItems.TryAdd(100001,
                TestHelpers.Instance.MapItemProvider!.Create(_session.Character.MapInstance, _item!.Create(1, 1, 6), 1,
                    1));

            await _getPacketHandler!.ExecuteAsync(new GetPacket
            {
                PickerId = _session.Character.CharacterId,
                VisualId = 100001,
                PickerType = VisualType.Player
            }, _session).ConfigureAwait(false);
            Assert.IsTrue(_session.Character.InventoryService.First().Value.ItemInstance!.Rare == 6);
        }

        [TestMethod]
        public async Task Test_Get_NotYourObjectAsync()
        {
            _session!.Character.PositionX = 0;
            _session.Character.PositionY = 0;
            var mapItem =
                TestHelpers.Instance.MapItemProvider!.Create(_session.Character.MapInstance!, _item!.Create(1012, 1), 1,
                    1);
            mapItem.VisualId = 1012;
            mapItem.OwnerId = 2;
            mapItem.DroppedAt = TestHelpers.Instance.Clock.GetCurrentInstant();
            _session.Character.MapInstance.MapItems.TryAdd(100001, mapItem);

            await _getPacketHandler!.ExecuteAsync(new GetPacket
            {
                PickerId = _session.Character.CharacterId,
                VisualId = 100001,
                PickerType = VisualType.Player
            }, _session).ConfigureAwait(false);
            var packet = (SayPacket?)_session.LastPackets.FirstOrDefault(s => s is SayPacket);
            Assert.IsTrue((packet?.Message == GameLanguage.Instance.GetMessageFromKey(LanguageKey.NOT_YOUR_ITEM,
                _session.Account.Language)) && (packet?.Type == SayColorType.Yellow));
            Assert.IsTrue(_session.Character.InventoryService!.Count == 0);
        }

        [TestMethod]
        public async Task Test_Get_NotYourObjectAfterDelayAsync()
        {
            _session!.Character.PositionX = 0;
            _session.Character.PositionY = 0;

            var mapItem =
                TestHelpers.Instance.MapItemProvider!.Create(_session.Character.MapInstance!, _item!.Create(1012, 1), 1,
                    1);
            mapItem.VisualId = 1012;
            mapItem.OwnerId = 2;
            mapItem.DroppedAt = TestHelpers.Instance.Clock.GetCurrentInstant().Plus(Duration.FromSeconds(-30));
            _session.Character.MapInstance.MapItems.TryAdd(100001, mapItem);

            await _getPacketHandler!.ExecuteAsync(new GetPacket
            {
                PickerId = _session.Character.CharacterId,
                VisualId = 100001,
                PickerType = VisualType.Player
            }, _session).ConfigureAwait(false);
            Assert.IsTrue(_session.Character.InventoryService!.Count > 0);
        }

        [TestMethod]
        public async Task Test_GetAwayAsync()
        {
            _session!.Character.PositionX = 7;
            _session.Character.PositionY = 7;

            _session.Character.MapInstance.MapItems.TryAdd(100001,
                TestHelpers.Instance.MapItemProvider!.Create(_session.Character.MapInstance, _item!.Create(1012, 1), 1,
                    1));
            await _getPacketHandler!.ExecuteAsync(new GetPacket
            {
                PickerId = _session.Character.CharacterId,
                VisualId = 100001,
                PickerType = VisualType.Player
            }, _session).ConfigureAwait(false);
            Assert.IsTrue(_session.Character.InventoryService!.Count == 0);
        }
    }
}