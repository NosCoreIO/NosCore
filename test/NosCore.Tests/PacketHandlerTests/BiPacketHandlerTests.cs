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
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.PacketHandlers.Inventory;
using NosCore.Tests.Helpers;
using Serilog;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class BiPacketHandlerTests
    {
        private static readonly ILogger Logger = Core.I18N.Logger.GetLoggerConfiguration().CreateLogger();
        private BiPacketHandler? _biPacketHandler;
        private IItemProvider? _item;
        private ClientSession? _session;

        [TestCleanup]
        public void Cleanup()
        {
            SystemTime.Freeze(SystemTime.Now());
        }

        [TestInitialize]
        public async Task SetupAsync()
        {
            SystemTime.Freeze();
            _session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            _item = TestHelpers.Instance.GenerateItemProvider();
            _biPacketHandler = new BiPacketHandler(Logger);
        }

        [TestMethod]
        public async Task Test_Delete_FromSlotAsync()
        {
            _session!.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item!.Create(1012, 999), 0));
            await _biPacketHandler!.ExecuteAsync(new BiPacket
                {Option = RequestDeletionType.Confirmed, Slot = 0, PocketType = PocketType.Main}, _session).ConfigureAwait(false);
            var packet = (IvnPacket?) _session.LastPackets.FirstOrDefault(s => s is IvnPacket);
            Assert.IsTrue(packet?.IvnSubPackets.All(iv => (iv?.Slot == 0) && (iv.VNum == -1)) ?? false);
        }

        [TestMethod]
        public async Task Test_Delete_FromEquimentAsync()
        {
            _session!.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item!.Create(1, 1), 0));
            await _biPacketHandler!.ExecuteAsync(new BiPacket
                {Option = RequestDeletionType.Confirmed, Slot = 0, PocketType = PocketType.Equipment}, _session).ConfigureAwait(false);
            Assert.IsTrue(_session.Character.InventoryService.Count == 0);
            var packet = (IvnPacket?) _session.LastPackets.FirstOrDefault(s => s is IvnPacket);
            Assert.IsTrue(packet?.IvnSubPackets.All(iv => (iv?.Slot == 0) && (iv.VNum == -1)) ?? false);
        }
    }
}