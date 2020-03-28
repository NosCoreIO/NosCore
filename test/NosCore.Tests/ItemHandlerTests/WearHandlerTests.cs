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

using System;
using System.Collections.Generic;
using System.Linq;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.Specialists;
using NosCore.Packets.ServerPackets.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.ItemProvider.Handlers;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.Tests.Helpers;
using Serilog;

namespace NosCore.Tests.ItemHandlerTests
{
    [TestClass]
    public class WearEventHandlerTests : UseItemEventHandlerTestsBase
    {
        private ItemProvider _itemProvider;
        private Mock<ILogger> _logger;

        [TestInitialize]
        public void Setup()
        {
            _logger = new Mock<ILogger>();
            TestHelpers.Instance.WorldConfiguration.BackpackSize = 40;
            _session = TestHelpers.Instance.GenerateSession();
            _handler = new WearEventHandler(_logger.Object);
            var items = new List<ItemDto>
            {
                new Item
                {
                    VNum = 1,
                    Type = NoscorePocketType.Equipment, 
                    ItemType = ItemType.Weapon,
                    RequireBinding = true
                },
                new Item
                { 
                    VNum = 2,
                    Type = NoscorePocketType.Equipment, 
                    EquipmentSlot = EquipmentType.Fairy,
                    Element = ElementType.Water
                },
                new Item
                {  
                    VNum = 3,
                    Type = NoscorePocketType.Equipment,
                    EquipmentSlot = EquipmentType.Fairy,
                    Element = ElementType.Fire
                },
                new Item
                { 
                    VNum = 4,
                    Type = NoscorePocketType.Equipment,
                    ItemType = ItemType.Specialist,
                    EquipmentSlot = EquipmentType.Sp,
                    Element = ElementType.Fire
                },
                new Item
                {
                    VNum = 5,
                    Type = NoscorePocketType.Equipment,
                    ItemType = ItemType.Weapon,
                    RequireBinding = true,
                    Sex = 2
                }, 
                new Item
                {
                    VNum = 6,
                    Type = NoscorePocketType.Equipment,
                    ItemType = ItemType.Specialist,
                    EquipmentSlot = EquipmentType.Sp,
                    LevelJobMinimum = 2,
                    Element = ElementType.Fire
                },   
                new Item
                {
                    VNum = 7,
                    Type = NoscorePocketType.Equipment,
                    ItemType = ItemType.Jewelery,
                    ItemValidTime = 50,
                    EquipmentSlot = EquipmentType.Amulet
                }
            };
            _itemProvider = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>());
        }

        [TestMethod]
        public void Test_Can_Not_Use_WearEvent_In_Shop()
        {
            _session.Character.InShop = true;
            var itemInstance = InventoryItemInstance.Create(_itemProvider.Create(1), _session.Character.CharacterId);
            ExecuteInventoryItemInstanceEventHandler(itemInstance);
            _logger.Verify(s => s.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CANT_USE_ITEM_IN_SHOP)), Times.Exactly(1));
        }

        [TestMethod]
        public void Test_BoundCharacter_Question()
        {
            var itemInstance = InventoryItemInstance.Create(_itemProvider.Create(1), _session.Character.CharacterId);
            _session.Character.InventoryService.AddItemToPocket(itemInstance);
            ExecuteInventoryItemInstanceEventHandler(itemInstance);
            var lastpacket = (QnaPacket)_session.LastPackets.FirstOrDefault(s => s is QnaPacket);
            Assert.AreEqual(_session.GetMessageFromKey(LanguageKey.ASK_BIND), lastpacket.Question);
        }

        [TestMethod]
        public void Test_BoundCharacter()
        {
            _useItem.Mode = 1;
            var itemInstance = InventoryItemInstance.Create(_itemProvider.Create(1), _session.Character.CharacterId);
            _session.Character.InventoryService.AddItemToPocket(itemInstance);
            ExecuteInventoryItemInstanceEventHandler(itemInstance);
            Assert.AreEqual(_session.Character.CharacterId, itemInstance.ItemInstance.BoundCharacterId);
        }

        [TestMethod]
        public void Test_BadEquipment()
        {
            _useItem.Mode = 1;
            var itemInstance = InventoryItemInstance.Create(_itemProvider.Create(5), _session.Character.CharacterId);
            _session.Character.InventoryService.AddItemToPocket(itemInstance);
            ExecuteInventoryItemInstanceEventHandler(itemInstance);
            var lastpacket = (SayPacket)_session.LastPackets.FirstOrDefault(s => s is SayPacket);
            Assert.AreEqual(_session.GetMessageFromKey(LanguageKey.BAD_EQUIPMENT), lastpacket.Message);
        }

        [TestMethod]
        public void Test_BadFairy()
        {
            _useItem.Mode = 1;
            _session.Character.UseSp = true;
            var itemInstance = InventoryItemInstance.Create(_itemProvider.Create(2), _session.Character.CharacterId);
            _session.Character.InventoryService.AddItemToPocket(itemInstance);
            var sp = InventoryItemInstance.Create(_itemProvider.Create(4), _session.Character.CharacterId);
            _session.Character.InventoryService.AddItemToPocket(sp, NoscorePocketType.Wear);
            ExecuteInventoryItemInstanceEventHandler(itemInstance);
            var lastpacket = (MsgPacket)_session.LastPackets.FirstOrDefault(s => s is MsgPacket);
            Assert.AreEqual(Language.Instance.GetMessageFromKey(LanguageKey.BAD_FAIRY,
                    _session.Account.Language), lastpacket.Message);
        }

        [TestMethod]
        public void Test_SpLoading()
        { 
            _useItem.Mode = 1;
            SystemTime.Freeze();
            _session.Character.LastSp = SystemTime.Now();
            _session.Character.SpCooldown = 300;
            var itemInstance = InventoryItemInstance.Create(_itemProvider.Create(4), _session.Character.CharacterId);
            _session.Character.InventoryService.AddItemToPocket(itemInstance);
            var sp = InventoryItemInstance.Create(_itemProvider.Create(4), _session.Character.CharacterId);
            _session.Character.InventoryService.AddItemToPocket(sp, NoscorePocketType.Wear);
            ExecuteInventoryItemInstanceEventHandler(itemInstance);
            var lastpacket = (MsgPacket)_session.LastPackets.FirstOrDefault(s => s is MsgPacket);
            Assert.AreEqual(string.Format(Language.Instance.GetMessageFromKey(LanguageKey.SP_INLOADING,
                    _session.Account.Language),
                _session.Character.SpCooldown), lastpacket.Message);
        }

        [TestMethod]
        public void Test_UseSp()
        {
            _useItem.Mode = 1;
            _session.Character.UseSp = true;
            var itemInstance = InventoryItemInstance.Create(_itemProvider.Create(4), _session.Character.CharacterId);
            _session.Character.InventoryService.AddItemToPocket(itemInstance);
            var sp = InventoryItemInstance.Create(_itemProvider.Create(4), _session.Character.CharacterId);
            _session.Character.InventoryService.AddItemToPocket(sp, NoscorePocketType.Wear);
            ExecuteInventoryItemInstanceEventHandler(itemInstance);
            var lastpacket = (SayPacket)_session.LastPackets.FirstOrDefault(s => s is SayPacket);
            Assert.AreEqual(
                Language.Instance.GetMessageFromKey(LanguageKey.SP_BLOCKED, _session.Account.Language), 
                lastpacket.Message);
        }

        [TestMethod]
        public void Test_UseDestroyedSp()
        {
            _useItem.Mode = 1;
            SystemTime.Freeze();
            var itemInstance = InventoryItemInstance.Create(_itemProvider.Create(4), _session.Character.CharacterId);
            _session.Character.InventoryService.AddItemToPocket(itemInstance);
            itemInstance.ItemInstance.Rare = -2;
            ExecuteInventoryItemInstanceEventHandler(itemInstance);
            var lastpacket = (MsgPacket)_session.LastPackets.FirstOrDefault(s => s is MsgPacket);
            Assert.AreEqual(Language.Instance.GetMessageFromKey(LanguageKey.CANT_EQUIP_DESTROYED_SP,
                    _session.Account.Language), lastpacket.Message);
        }

        [TestMethod]
        public void Test_Use_BadJobLevel()
        {
            _useItem.Mode = 1;
            SystemTime.Freeze();
            var itemInstance = InventoryItemInstance.Create(_itemProvider.Create(6), _session.Character.CharacterId);
            _session.Character.InventoryService.AddItemToPocket(itemInstance);
            ExecuteInventoryItemInstanceEventHandler(itemInstance);
            var lastpacket = (SayPacket)_session.LastPackets.FirstOrDefault(s => s is SayPacket);
            Assert.AreEqual(Language.Instance.GetMessageFromKey(LanguageKey.LOW_JOB_LVL,
                    _session.Account.Language), lastpacket.Message);
        }

        [TestMethod]
        public void Test_Use_SP()
        {
            _useItem.Mode = 1;
            SystemTime.Freeze();
            var itemInstance = InventoryItemInstance.Create(_itemProvider.Create(4), _session.Character.CharacterId);
            _session.Character.InventoryService.AddItemToPocket(itemInstance);
            ExecuteInventoryItemInstanceEventHandler(itemInstance);
            var lastpacket = (SpPacket)_session.LastPackets.FirstOrDefault(s => s is SpPacket);
            Assert.IsNotNull(lastpacket);
        }

        [TestMethod]
        public void Test_Use_Fairy()
        {
            _useItem.Mode = 1;
            SystemTime.Freeze();
            var itemInstance = InventoryItemInstance.Create(_itemProvider.Create(2), _session.Character.CharacterId);
            _session.Character.InventoryService.AddItemToPocket(itemInstance);
            ExecuteInventoryItemInstanceEventHandler(itemInstance);
            var lastpacket = (PairyPacket)_session.Character.MapInstance.LastPackets.FirstOrDefault(s => s is PairyPacket);
            Assert.IsNotNull(lastpacket);
        }

        [TestMethod]
        public void Test_Use_Amulet()
        {
            _useItem.Mode = 1;
            SystemTime.Freeze();
            var itemInstance = InventoryItemInstance.Create(_itemProvider.Create(7), _session.Character.CharacterId);
            _session.Character.InventoryService.AddItemToPocket(itemInstance);
            ExecuteInventoryItemInstanceEventHandler(itemInstance);
            var lastpacket = (EffectPacket)_session.LastPackets.FirstOrDefault(s => s is EffectPacket);
            Assert.IsNotNull(lastpacket);
            Assert.AreEqual(SystemTime.Now().AddSeconds(itemInstance.ItemInstance.Item.ItemValidTime), itemInstance.ItemInstance.ItemDeleteTime);
        }
    }
}