using System;
using System.Linq;
using ChickenAPI.Packets.ClientPackets.Specialists;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.PacketHandlers.Inventory;
using NosCore.Tests.Helpers;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class SpTransformPacketHandlerTests
    {
        private SpTransformPacketHandler _spTransformPacketHandler;
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
            _spTransformPacketHandler = new SpTransformPacketHandler();
        }


        [TestMethod]
        public void Test_Transform_NoSp()
        {
            _spTransformPacketHandler.Execute(new SpTransformPacket { Type = SlPacketType.WearSp }, _session);
            var packet = (MsgPacket)_session.LastPacket;
            Assert.IsTrue(packet.Message ==
                Language.Instance.GetMessageFromKey(LanguageKey.NO_SP, _session.Account.Language));
        }

        [TestMethod]
        public void Test_Transform_Vehicle()
        {
            _session.Character.IsVehicled = true;
            _session.Character.Inventory.AddItemToPocket(InventoryItemInstance.Create(_item.Create(912, 1), _session.Character.CharacterId));
            var item = _session.Character.Inventory.First();
            item.Value.Type = NoscorePocketType.Wear;
            item.Value.Slot = (byte)EquipmentType.Sp;
            _spTransformPacketHandler.Execute(new SpTransformPacket { Type = SlPacketType.WearSp }, _session);
            var packet = (MsgPacket)_session.LastPacket;
            Assert.IsTrue(packet.Message ==
                Language.Instance.GetMessageFromKey(LanguageKey.REMOVE_VEHICLE, _session.Account.Language));
        }


        [TestMethod]
        public void Test_Transform_Sitted()
        {
            _session.Character.IsSitting = true;
            _spTransformPacketHandler.Execute(new SpTransformPacket { Type = SlPacketType.WearSp }, _session);
            Assert.IsNull(_session.LastPacket);
        }

        [TestMethod]
        public void Test_RemoveSp()
        {
            _session.Character.Inventory.AddItemToPocket(InventoryItemInstance.Create(_item.Create(912, 1), _session.Character.CharacterId));
            var item = _session.Character.Inventory.First();
            _session.Character.UseSp = true;
            item.Value.Type = NoscorePocketType.Wear;
            item.Value.Slot = (byte)EquipmentType.Sp;
            _spTransformPacketHandler.Execute(new SpTransformPacket { Type = (SlPacketType)1 }, _session);
            Assert.IsFalse(_session.Character.UseSp);
        }

        [TestMethod]
        public void Test_Transform()
        {
            _session.Character.SpPoint = 1;
            _session.Character.Reput = 5000000;
            _session.Character.Inventory.AddItemToPocket(InventoryItemInstance.Create(_item.Create(912, 1), _session.Character.CharacterId));
            var item = _session.Character.Inventory.First();
            item.Value.Type = NoscorePocketType.Wear;
            item.Value.Slot = (byte)EquipmentType.Sp;
            _spTransformPacketHandler.Execute(new SpTransformPacket { Type = (SlPacketType)1 }, _session);
            Assert.IsTrue(_session.Character.UseSp);
        }

        [TestMethod]
        public void Test_Transform_BadFairy()
        {
            _session.Character.SpPoint = 1;
            _session.Character.Reput = 5000000;
            var item = _session.Character.Inventory.AddItemToPocket(InventoryItemInstance.Create(_item.Create(912, 1), _session.Character.CharacterId)).First();
            var fairy = _session.Character.Inventory.AddItemToPocket(InventoryItemInstance.Create(_item.Create(2, 1), _session.Character.CharacterId)).First();

            item.Type = NoscorePocketType.Wear;
            item.Slot = (byte)EquipmentType.Sp;
            fairy.Type = NoscorePocketType.Wear;
            fairy.Slot = (byte)EquipmentType.Fairy;
            _spTransformPacketHandler.Execute(new SpTransformPacket { Type = (SlPacketType)1 }, _session);
            var packet = (MsgPacket)_session.LastPacket;
            Assert.IsTrue(packet.Message ==
                Language.Instance.GetMessageFromKey(LanguageKey.BAD_FAIRY, _session.Account.Language));
        }

        [TestMethod]
        public void Test_Transform_BadReput()
        {
            _session.Character.SpPoint = 1;
            _session.Character.Inventory.AddItemToPocket(InventoryItemInstance.Create(_item.Create(912, 1), _session.Character.CharacterId));
            var item = _session.Character.Inventory.First();
            item.Value.Type = NoscorePocketType.Wear;
            item.Value.Slot = (byte)EquipmentType.Sp;
            _spTransformPacketHandler.Execute(new SpTransformPacket { Type = (SlPacketType)1 }, _session);
            var packet = (MsgPacket)_session.LastPacket;
            Assert.IsTrue(packet.Message ==
                Language.Instance.GetMessageFromKey(LanguageKey.LOW_REP, _session.Account.Language));
        }


        [TestMethod]
        public void Test_TransformBefore_Cooldown()
        {
            _session.Character.SpPoint = 1;
            _session.Character.LastSp = SystemTime.Now();
            _session.Character.SpCooldown = 30;
            _session.Character.Inventory.AddItemToPocket(InventoryItemInstance.Create(_item.Create(912, 1), _session.Character.CharacterId));
            var item = _session.Character.Inventory.First();
            item.Value.Type = NoscorePocketType.Wear;
            item.Value.Slot = (byte)EquipmentType.Sp;
            _spTransformPacketHandler.Execute(new SpTransformPacket { Type = (SlPacketType)1 }, _session);
            var packet = (MsgPacket)_session.LastPacket;
            Assert.IsTrue(packet.Message ==
                string.Format(Language.Instance.GetMessageFromKey(LanguageKey.SP_INLOADING, _session.Account.Language),
                    30));
        }

        [TestMethod]
        public void Test_Transform_OutOfSpPoint()
        {
            _session.Character.LastSp = SystemTime.Now();
            _session.Character.Inventory.AddItemToPocket(InventoryItemInstance.Create(_item.Create(912, 1), _session.Character.CharacterId));
            var item = _session.Character.Inventory.First();
            item.Value.Type = NoscorePocketType.Wear;
            item.Value.Slot = (byte)EquipmentType.Sp;
            _spTransformPacketHandler.Execute(new SpTransformPacket { Type = (SlPacketType)1 }, _session);
            var packet = (MsgPacket)_session.LastPacket;
            Assert.IsTrue(packet.Message ==
                Language.Instance.GetMessageFromKey(LanguageKey.SP_NOPOINTS, _session.Account.Language));
        }

        [TestMethod]
        public void Test_Transform_Delay()
        {
            _session.Character.SpPoint = 1;
            _session.Character.LastSp = SystemTime.Now();
            _session.Character.Inventory.AddItemToPocket(InventoryItemInstance.Create(_item.Create(912, 1), _session.Character.CharacterId));
            var item = _session.Character.Inventory.First();
            item.Value.Type = NoscorePocketType.Wear;
            item.Value.Slot = (byte)EquipmentType.Sp;
            _spTransformPacketHandler.Execute(new SpTransformPacket { Type = SlPacketType.WearSp }, _session);
            var packet = (DelayPacket)_session.LastPacket;
            Assert.IsTrue(packet.Delay == 5000);
        }
    }
}
