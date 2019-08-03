using System.Linq;
using ChickenAPI.Packets.ClientPackets.Inventory;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.Inventory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core;
using NosCore.Core.I18N;
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
        private BiPacketHandler _biPacketHandler;
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private IItemProvider _item;
        private ClientSession _session;

        [TestCleanup]
        public void Cleanup()
        {
            SystemTime.Freeze(SystemTime.Now());
        }

        [TestInitialize]
        public void Setup()
        {
            SystemTime.Freeze();
            _session = TestHelpers.Instance.GenerateSession();
            _item = TestHelpers.Instance.GenerateItemProvider();
            _biPacketHandler = new BiPacketHandler(_logger);
        }
        [TestMethod]
        public void Test_Delete_FromSlot()
        {
            _session.Character.Inventory.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1012, 999), 0));
            _biPacketHandler.Execute(new BiPacket
            { Option = RequestDeletionType.Confirmed, Slot = 0, PocketType = PocketType.Main }, _session);
            var packet = (IvnPacket)_session.LastPacket.FirstOrDefault(s => s is IvnPacket);
            Assert.IsTrue(packet.IvnSubPackets.All(iv => iv.Slot == 0 && iv.VNum == -1));
        }

        [TestMethod]
        public void Test_Delete_FromEquiment()
        {
            _session.Character.Inventory.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1), 0));
            _biPacketHandler.Execute(new BiPacket
            { Option = RequestDeletionType.Confirmed, Slot = 0, PocketType = PocketType.Equipment }, _session);
            Assert.IsTrue(_session.Character.Inventory.Count == 0);
            var packet = (IvnPacket)_session.LastPacket.FirstOrDefault(s => s is IvnPacket);
            Assert.IsTrue(packet.IvnSubPackets.All(iv => iv.Slot == 0 && iv.VNum == -1));
        }

    }
}
