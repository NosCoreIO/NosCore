using System.Linq;
using ChickenAPI.Packets.ClientPackets.Drops;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.Chats;
using ChickenAPI.Packets.ServerPackets.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.PacketHandlers.Inventory;
using NosCore.Tests.Helpers;
using Serilog;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class GetPacketHandlerTests
    {
        private GetPacketHandler _getPacketHandler;
        private static ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private ClientSession _session;
        private IItemProvider _item;

        [TestCleanup]
        public void Cleanup()
        {
            SystemTime.Freeze(SystemTime.Now());
        }

        [TestInitialize]
        public void Setup()
        {
            SystemTime.Freeze();
            TestHelpers.Reset();
            _item = TestHelpers.Instance.GenerateItemProvider();
            _session = TestHelpers.Instance.GenerateSession();
            _getPacketHandler = new GetPacketHandler(_logger);
        }


        [TestMethod]
        public void Test_Get()
        {
            _session.Character.PositionX = 0;
            _session.Character.PositionY = 0;
            _session.Character.MapInstance.MapItems.TryAdd(100001, TestHelpers.Instance.MapItemProvider.Create(_session.Character.MapInstance, _item.Create(1012, 1), 1, 1));

            _getPacketHandler.Execute(new GetPacket
            {
                PickerId = _session.Character.CharacterId,
                VisualId = 100001,
                PickerType = VisualType.Player
            }, _session);
            Assert.IsTrue(_session.Character.Inventory.Count > 0);
        }

        [TestMethod]
        public void Test_GetInStack()
        {
            _session.Character.PositionX = 0;
            _session.Character.PositionY = 0;

            _session.Character.MapInstance.MapItems.TryAdd(100001, TestHelpers.Instance.MapItemProvider.Create(_session.Character.MapInstance, _item.Create(1012, 1), 1, 1));
            _session.Character.Inventory.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1012, 1), 0));
            _getPacketHandler.Execute(new GetPacket
            {
                PickerId = _session.Character.CharacterId,
                VisualId = 100001,
                PickerType = VisualType.Player
            }, _session);
            Assert.IsTrue(_session.Character.Inventory.First().Value.ItemInstance.Amount == 2);
        }

        [TestMethod]
        public void Test_GetFullInventory()
        {
            _session.Character.PositionX = 0;
            _session.Character.PositionY = 0;
            _session.Character.MapInstance.MapItems.TryAdd(100001, TestHelpers.Instance.MapItemProvider.Create(_session.Character.MapInstance, _item.Create(1, 1), 1, 1));
            _session.Character.Inventory.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1), 0));
            _session.Character.Inventory.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1), 0));
            _getPacketHandler.Execute(new GetPacket
            {
                PickerId = _session.Character.CharacterId,
                VisualId = 100001,
                PickerType = VisualType.Player
            }, _session);
            var packet = (MsgPacket)_session.LastPacket.FirstOrDefault(s => s is MsgPacket);
            Assert.IsTrue(packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_PLACE,
                _session.Account.Language) && packet.Type == 0);
            Assert.IsTrue(_session.Character.Inventory.Count == 2);
        }

        [TestMethod]
        public void Test_Get_KeepRarity()
        {
            _session.Character.PositionX = 0;
            _session.Character.PositionY = 0;
            _session.Character.MapInstance.MapItems.TryAdd(100001, TestHelpers.Instance.MapItemProvider.Create(_session.Character.MapInstance, _item.Create(1, 1, 6), 1, 1));

            _getPacketHandler.Execute(new GetPacket
            {
                PickerId = _session.Character.CharacterId,
                VisualId = 100001,
                PickerType = VisualType.Player
            }, _session);
            Assert.IsTrue(_session.Character.Inventory.First().Value.ItemInstance.Rare == 6);
        }

        [TestMethod]
        public void Test_Get_NotYourObject()
        {
            _session.Character.PositionX = 0;
            _session.Character.PositionY = 0;
            var mapItem = TestHelpers.Instance.MapItemProvider.Create(_session.Character.MapInstance, _item.Create(1012, 1), 1, 1);
            mapItem.VisualId = 1012;
            mapItem.OwnerId = 2;
            mapItem.DroppedAt = SystemTime.Now();
            _session.Character.MapInstance.MapItems.TryAdd(100001, mapItem);

            _getPacketHandler.Execute(new GetPacket
            {
                PickerId = _session.Character.CharacterId,
                VisualId = 100001,
                PickerType = VisualType.Player
            }, _session);
            var packet = (SayPacket)_session.LastPacket.FirstOrDefault(s => s is SayPacket);
            Assert.IsTrue(packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.NOT_YOUR_ITEM,
                _session.Account.Language) && packet.Type == SayColorType.Yellow);
            Assert.IsTrue(_session.Character.Inventory.Count == 0);
        }

        [TestMethod]
        public void Test_Get_NotYourObjectAfterDelay()
        {
            _session.Character.PositionX = 0;
            _session.Character.PositionY = 0;

            var mapItem = TestHelpers.Instance.MapItemProvider.Create(_session.Character.MapInstance, _item.Create(1012, 1), 1, 1);
            mapItem.VisualId = 1012;
            mapItem.OwnerId = 2;
            mapItem.DroppedAt = SystemTime.Now().AddSeconds(-30);
            _session.Character.MapInstance.MapItems.TryAdd(100001, mapItem);

            _getPacketHandler.Execute(new GetPacket
            {
                PickerId = _session.Character.CharacterId,
                VisualId = 100001,
                PickerType = VisualType.Player
            }, _session);
            Assert.IsTrue(_session.Character.Inventory.Count > 0);
        }

        [TestMethod]
        public void Test_GetAway()
        {
            _session.Character.PositionX = 7;
            _session.Character.PositionY = 7;

            _session.Character.MapInstance.MapItems.TryAdd(100001, TestHelpers.Instance.MapItemProvider.Create(_session.Character.MapInstance, _item.Create(1012, 1), 1, 1));
            _getPacketHandler.Execute(new GetPacket
            {
                PickerId = _session.Character.CharacterId,
                VisualId = 100001,
                PickerType = VisualType.Player
            }, _session);
            Assert.IsTrue(_session.Character.Inventory.Count == 0);
        }

    }
}
