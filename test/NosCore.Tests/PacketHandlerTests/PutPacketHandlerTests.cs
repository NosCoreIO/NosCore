using System;
using System.Collections.Generic;
using System.Linq;
using ChickenAPI.Packets.ClientPackets.Drops;
using ChickenAPI.Packets.ClientPackets.Inventory;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Encryption;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Map;
using NosCore.Database;
using NosCore.Database.DAL;
using NosCore.GameObject;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.ExchangeProvider;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.ItemProvider.Handlers;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.GameObject.Providers.MapInstanceProvider;
using NosCore.GameObject.Providers.MapItemProvider;
using NosCore.GameObject.Providers.MapItemProvider.Handlers;
using NosCore.PacketHandlers.Inventory;
using NosCore.Tests.Helpers;
using Serilog;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class PutPacketHandlerTests
    {
        private PutPacketHandler _putPacketHandler;
        private ClientSession _session;
        private IItemProvider _item;

        [TestCleanup]
        public void Cleanup()
        {
            SystemTime.Freeze(DateTime.Now);
        }

        [TestInitialize]
        public void Setup()
        {
            SystemTime.Freeze();
            TestHelpers.Reset();
            _item = TestHelpers.Instance.GenerateItemProvider();
            _session = TestHelpers.Instance.GenerateSession();
            _putPacketHandler = new PutPacketHandler(_session.WorldConfiguration);
        }

        [TestMethod]
        public void Test_PutPartialSlot()
        {
            _session.Character.Inventory.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1012, 999),0));
            _putPacketHandler.Execute(new PutPacket
            {
                NoscorePocketType = NoscorePocketType.Main,
                Slot = 0,
                Amount = 500
            }, _session);
            Assert.IsTrue(_session.Character.Inventory.Count == 1 &&
                _session.Character.Inventory.FirstOrDefault().Value.ItemInstance.Amount == 499);
        }

        [TestMethod]
        public void Test_PutNotDroppable()
        {
            _session.Character.Inventory.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1013, 1),0));
            _putPacketHandler.Execute(new PutPacket
            {
                NoscorePocketType = NoscorePocketType.Main,
                Slot = 0,
                Amount = 1
            }, _session);
            var packet = (MsgPacket)_session.LastPacket;
            Assert.IsTrue(packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.ITEM_NOT_DROPPABLE,
                _session.Account.Language) && packet.Type == 0);
            Assert.IsTrue(_session.Character.Inventory.Count > 0);
        }


        [TestMethod]
        public void Test_Put()
        {
            _session.Character.Inventory.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1012, 1),0));
            _putPacketHandler.Execute(new PutPacket
            {
                NoscorePocketType = NoscorePocketType.Main,
                Slot = 0,
                Amount = 1
            }, _session);
            Assert.IsTrue(_session.Character.Inventory.Count == 0);
        }

        [TestMethod]
        public void Test_PutBadPlace()
        {
            _session.Character.PositionX = 2;
            _session.Character.PositionY = 2;
            _session.Character.Inventory.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1012, 1),0));
            _putPacketHandler.Execute(new PutPacket
            {
                NoscorePocketType = NoscorePocketType.Main,
                Slot = 0,
                Amount = 1
            }, _session);
            var packet = (MsgPacket)_session.LastPacket;
            Assert.IsTrue(packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.ITEM_NOT_DROPPABLE_HERE,
                _session.Account.Language) && packet.Type == 0);
            Assert.IsTrue(_session.Character.Inventory.Count > 0);
        }

        [TestMethod]
        public void Test_PutOutOfBounds()
        {
            _session.Character.PositionX = -1;
            _session.Character.PositionY = -1;
            _session.Character.Inventory.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1012, 1),0));
            _putPacketHandler.Execute(new PutPacket
            {
                NoscorePocketType = NoscorePocketType.Main,
                Slot = 0,
                Amount = 1
            }, _session);
            var packet = (MsgPacket)_session.LastPacket;
            Assert.IsTrue(packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.ITEM_NOT_DROPPABLE_HERE,
                _session.Account.Language) && packet.Type == 0);
            Assert.IsTrue(_session.Character.Inventory.Count > 0);
        }
    }
}
