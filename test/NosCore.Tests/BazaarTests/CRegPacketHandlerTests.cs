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
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ServerPackets.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.HttpClients.BazaarHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.PacketHandlers.Bazaar;
using NosCore.Tests.Helpers;

namespace NosCore.Tests.BazaarTests
{
    [TestClass]
    public class CRegPacketHandlerTest
    {
        private Mock<IBazaarHttpClient> _bazaarHttpClient;
        private CRegPacketHandler _cregPacketHandler;
        private Mock<IGenericDao<InventoryItemInstanceDto>> _inventoryItemInstanceDao;
        private Mock<IGenericDao<IItemInstanceDto>> _itemInstanceDao;
        private ItemProvider _itemProvider;
        private ClientSession _session;

        [TestInitialize]
        public void Setup()
        {
            TestHelpers.Reset();
            Broadcaster.Reset();
            _session = TestHelpers.Instance.GenerateSession();
            _session.Character.StaticBonusList = new List<StaticBonusDto>();
            _bazaarHttpClient = new Mock<IBazaarHttpClient>();
            _inventoryItemInstanceDao = new Mock<IGenericDao<InventoryItemInstanceDto>>();
            _itemInstanceDao = new Mock<IGenericDao<IItemInstanceDto>>();
            _bazaarHttpClient.Setup(s => s.AddBazaar(It.IsAny<BazaarRequest>())).Returns(LanguageKey.OBJECT_IN_BAZAAR);
            var items = new List<ItemDto>
            {
                new Item {Type = NoscorePocketType.Main, VNum = 1012, IsSoldable = true},
                new Item {Type = NoscorePocketType.Main, VNum = 1013},
                new Item {Type = NoscorePocketType.Equipment, VNum = 1, ItemType = ItemType.Weapon},
                new Item {Type = NoscorePocketType.Equipment, VNum = 2, ItemType = ItemType.Weapon},
                new Item {Type = NoscorePocketType.Equipment, VNum = 912, ItemType = ItemType.Specialist},
                new Item {Type = NoscorePocketType.Equipment, VNum = 924, ItemType = ItemType.Fashion}
            };
            _itemProvider = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>());
            _cregPacketHandler = new CRegPacketHandler(TestHelpers.Instance.WorldConfiguration,
                _bazaarHttpClient.Object, _itemInstanceDao.Object, _inventoryItemInstanceDao.Object);
        }

        [TestMethod]
        public void RegisterWhenInExchangeOrTrade()
        {
            _session.Character.InExchangeOrTrade = true;
            _cregPacketHandler.Execute(new CRegPacket
            {
                Type = 0,
                Inventory = 0,
                Slot = 0,
                Durability = 0,
                IsPackage = 0,
                Amount = 1,
                Taxe = 0,
                MedalUsed = 0
            }, _session);

            Assert.IsNull(_session.LastPackets.FirstOrDefault());
        }


        [TestMethod]
        public void RegisterTaxWhenMedalMoreThanGold()
        {
            _cregPacketHandler.Execute(new CRegPacket
            {
                Type = 0,
                Inventory = 0,
                Slot = 0,
                Durability = 0,
                IsPackage = 0,
                Amount = 1,
                Taxe = 0,
                MedalUsed = 0
            }, _session);

            var lastpacket = (MsgPacket) _session.LastPackets.FirstOrDefault(s => s is MsgPacket);
            Assert.IsTrue(lastpacket.Message ==
                Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_MONEY, _session.Account.Language));
        }

        [TestMethod]
        public void RegisterNegativeAmount()
        {
            _session.Character.Gold = 500000;
            _cregPacketHandler.Execute(new CRegPacket
            {
                Type = 0,
                Inventory = 0,
                Slot = 0,
                Durability = 0,
                IsPackage = 0,
                Amount = -1,
                Taxe = 0,
                MedalUsed = 0
            }, _session);
            Assert.IsNull(_session.LastPackets.FirstOrDefault());
        }

        [TestMethod]
        public void RegisterNotExistingItem()
        {
            _session.Character.Gold = 500000;
            _cregPacketHandler.Execute(new CRegPacket
            {
                Type = 0,
                Inventory = 1,
                Slot = 0,
                Durability = 0,
                IsPackage = 0,
                Amount = 1,
                Taxe = 0,
                MedalUsed = 0
            }, _session);
            Assert.IsNull(_session.LastPackets.FirstOrDefault());
        }

        [TestMethod]
        public void RegisterTooExpensiveWhenNoMedal()
        {
            _session.Character.Gold = 500000;
            _session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012), 0))
                .First();
            _cregPacketHandler.Execute(new CRegPacket
            {
                Type = 0,
                Inventory = 1,
                Slot = 0,
                Durability = 0,
                IsPackage = 0,
                Amount = 1,
                Taxe = 0,
                MedalUsed = 0,
                Price = 100000001
            }, _session);
            var lastpacket = (MsgPacket) _session.LastPackets.FirstOrDefault(s => s is MsgPacket);
            Assert.IsTrue(lastpacket.Message ==
                Language.Instance.GetMessageFromKey(LanguageKey.PRICE_EXCEEDED, _session.Account.Language));
        }

        [TestMethod]
        public void RegisterHasSmallerTaxWhenMedal()
        {
            _session.Character.Gold = 100000;
            _session.Character.StaticBonusList.Add(new StaticBonusDto
            {
                StaticBonusType = StaticBonusType.BazaarMedalGold
            });
            _session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012), 0))
                .First();
            _cregPacketHandler.Execute(new CRegPacket
            {
                Type = 0,
                Inventory = 1,
                Slot = 0,
                Durability = 1,
                IsPackage = 0,
                Amount = 1,
                Taxe = 0,
                MedalUsed = 0,
                Price = 10000000
            }, _session);
            var lastpacket = (MsgPacket) _session.LastPackets.FirstOrDefault(s => s is MsgPacket);
            Assert.AreEqual(0, _session.Character.InventoryService.Count);
            Assert.IsTrue(lastpacket.Message ==
                Language.Instance.GetMessageFromKey(LanguageKey.OBJECT_IN_BAZAAR, _session.Account.Language));
        }

        [TestMethod]
        public void RegisterTooExpensive()
        {
            _session.Character.Gold = 5000000;
            _session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012), 0))
                .First();
            _cregPacketHandler.Execute(new CRegPacket
            {
                Type = 0,
                Inventory = 1,
                Slot = 0,
                Durability = 0,
                IsPackage = 0,
                Amount = 1,
                Taxe = 0,
                MedalUsed = 0,
                Price = TestHelpers.Instance.WorldConfiguration.MaxGoldAmount + 1
            }, _session);
            var lastpacket = (MsgPacket) _session.LastPackets.FirstOrDefault(s => s is MsgPacket);
            Assert.IsTrue(lastpacket.Message ==
                Language.Instance.GetMessageFromKey(LanguageKey.PRICE_EXCEEDED, _session.Account.Language));
        }

        [TestMethod]
        public void RegisterTooLongWhenNoMedal()
        {
            _session.Character.Gold = 5000000;
            _session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012), 0))
                .First();
            _cregPacketHandler.Execute(new CRegPacket
            {
                Type = 0,
                Inventory = 1,
                Slot = 0,
                Durability = 2,
                IsPackage = 0,
                Amount = 1,
                Taxe = 0,
                MedalUsed = 0,
                Price = 1
            }, _session);
            Assert.IsNull(_session.LastPackets.FirstOrDefault());
        }


        [TestMethod]
        public void RegisterUnvalidTime()
        {
            _session.Character.Gold = 5000000;
            _session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012), 0))
                .First();
            _cregPacketHandler.Execute(new CRegPacket
            {
                Type = 0,
                Inventory = 1,
                Slot = 0,
                Durability = 7,
                IsPackage = 0,
                Amount = 1,
                Taxe = 0,
                MedalUsed = 0,
                Price = 1
            }, _session);
            Assert.IsNull(_session.LastPackets.FirstOrDefault());
        }

        [TestMethod]
        public void RegisterLimitExceeded()
        {
            _session.Character.Gold = 5000000;
            _session.Character.InventoryService
                .AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 999), 0)).First();
            _bazaarHttpClient.Reset();
            _bazaarHttpClient.Setup(s => s.AddBazaar(It.IsAny<BazaarRequest>())).Returns(LanguageKey.LIMIT_EXCEEDED);
            _cregPacketHandler.Execute(new CRegPacket
            {
                Type = 0,
                Inventory = 1,
                Slot = 0,
                Durability = 1,
                IsPackage = 0,
                Amount = 949,
                Taxe = 0,
                MedalUsed = 0,
                Price = 1
            }, _session);
            var lastpacket = (MsgPacket) _session.LastPackets.FirstOrDefault(s => s is MsgPacket);
            Assert.AreEqual(999, _session.Character.InventoryService.FirstOrDefault().Value.ItemInstance.Amount);
            Assert.IsTrue(lastpacket.Message ==
                Language.Instance.GetMessageFromKey(LanguageKey.LIMIT_EXCEEDED, _session.Account.Language));
        }

        [TestMethod]
        public void RegisterAllSlot()
        {
            _session.Character.Gold = 5000000;
            _session.Character.InventoryService
                .AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 999), 0)).First();
            _cregPacketHandler.Execute(new CRegPacket
            {
                Type = 0,
                Inventory = 1,
                Slot = 0,
                Durability = 1,
                IsPackage = 0,
                Amount = 999,
                Taxe = 0,
                MedalUsed = 0,
                Price = 1
            }, _session);
            var lastpacket = (MsgPacket) _session.LastPackets.FirstOrDefault(s => s is MsgPacket);
            Assert.AreEqual(0, _session.Character.InventoryService.Count);
            Assert.IsTrue(lastpacket.Message ==
                Language.Instance.GetMessageFromKey(LanguageKey.OBJECT_IN_BAZAAR, _session.Account.Language));
        }

        [TestMethod]
        public void RegisterLessThanInInventory()
        {
            _session.Character.Gold = 5000000;
            _session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012), 0))
                .First();
            _cregPacketHandler.Execute(new CRegPacket
            {
                Type = 0,
                Inventory = 1,
                Slot = 0,
                Durability = 1,
                IsPackage = 0,
                Amount = 2,
                Taxe = 0,
                MedalUsed = 0,
                Price = 1
            }, _session);
            Assert.IsNull(_session.LastPackets.FirstOrDefault());
        }

        [TestMethod]
        public void RegisterPartialSlot()
        {
            _session.Character.Gold = 5000000;
            _session.Character.InventoryService
                .AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 999), 0)).First();
            _cregPacketHandler.Execute(new CRegPacket
            {
                Type = 0,
                Inventory = 1,
                Slot = 0,
                Durability = 1,
                IsPackage = 0,
                Amount = 949,
                Taxe = 0,
                MedalUsed = 0,
                Price = 1
            }, _session);
            var lastpacket = (MsgPacket) _session.LastPackets.FirstOrDefault(s => s is MsgPacket);
            Assert.AreEqual(50, _session.Character.InventoryService.FirstOrDefault().Value.ItemInstance.Amount);
            Assert.IsTrue(lastpacket.Message ==
                Language.Instance.GetMessageFromKey(LanguageKey.OBJECT_IN_BAZAAR, _session.Account.Language));
        }
    }
}