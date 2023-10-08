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
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.PacketHandlers.Inventory;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Tests.Shared;
using Serilog;

namespace NosCore.PacketHandlers.Tests.Inventory
{
    [TestClass]
    public class BiPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private BiPacketHandler? _biPacketHandler;
        private IItemGenerationService? _item;
        private ClientSession? _session;
        
        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            _item = TestHelpers.Instance.GenerateItemProvider();
            _biPacketHandler = new BiPacketHandler(Logger, TestHelpers.Instance.LogLanguageLocalizer);
        }

        [TestMethod]
        public async Task Test_Delete_FromSlotAsync()
        {
            _session!.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item!.Create(1012, 999), 0));
            await _biPacketHandler!.ExecuteAsync(new BiPacket
            { Option = RequestDeletionType.Confirmed, Slot = 0, PocketType = PocketType.Main }, _session).ConfigureAwait(false);
            var packet = (IvnPacket?)_session.LastPackets.FirstOrDefault(s => s is IvnPacket);
            Assert.IsTrue(packet?.IvnSubPackets?.All(iv => (iv?.Slot == 0) && (iv.VNum == -1)) ?? false);
        }

        [TestMethod]
        public async Task Test_Delete_FromEquimentAsync()
        {
            _session!.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item!.Create(1, 1), 0));
            await _biPacketHandler!.ExecuteAsync(new BiPacket
            { Option = RequestDeletionType.Confirmed, Slot = 0, PocketType = PocketType.Equipment }, _session).ConfigureAwait(false);
            Assert.IsTrue(_session.Character.InventoryService.Count == 0);
            var packet = (IvnPacket?)_session.LastPackets.FirstOrDefault(s => s is IvnPacket);
            Assert.IsTrue(packet?.IvnSubPackets?.All(iv => (iv?.Slot == 0) && (iv.VNum == -1)) ?? false);
        }
    }
}