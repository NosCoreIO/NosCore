//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Configuration;
using NosCore.Controllers;
using NosCore.Core.Encryption;
using NosCore.Core.Serializing;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Data.StaticEntities;
using NosCore.Database;
using NosCore.DAL;
using NosCore.GameObject;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.Inventory;
using NosCore.GameObject.Services.ItemBuilder;
using NosCore.GameObject.Services.ItemBuilder.Item;
using NosCore.GameObject.Services.MapInstanceAccess;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.Enumerations.Interaction;
using NosCore.Shared.Enumerations.Items;
using NosCore.Shared.Enumerations.Map;
using NosCore.Shared.I18N;

namespace NosCore.Tests.HandlerTests
{
    [TestClass]
    public class InventoryPacketControllerTests
    {
        private readonly ClientSession _session = new ClientSession(null,
            new List<PacketController> { new InventoryPacketController() }, null);
        
        private CharacterDto _chara;
        private InventoryPacketController _handler;
        private ItemBuilderService _itemBuilder;
        private readonly MapInstance _map = new MapInstance(new Map
            {
            Name = "testMap",
            Data = new byte[]
                {
                    8, 0, 8, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 1, 1, 1, 0, 0, 0, 0,
                    0, 1, 1, 1, 0, 0, 0, 0, 
                    0, 1, 1, 1, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0
                }
        }
            , Guid.NewGuid(), false, MapInstanceType.BaseMapInstance, new List<NpcMonsterDto>());

        [TestInitialize]
        public void Setup()
        {
            PacketFactory.Initialize<NoS0575Packet>();
            var contextBuilder =
                new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(
                    databaseName: Guid.NewGuid().ToString());
            DataAccessHelper.Instance.InitializeForTest(contextBuilder.Options);
            var _acc = new AccountDto { Name = "AccountTest", Password = EncryptionHelper.Sha512("test") };
            _chara = new CharacterDto
            {
                CharacterId = 1,
                Name = "TestExistingCharacter",
                Slot = 1,
                AccountId = _acc.AccountId,
                MapId = 1,
                State = CharacterState.Active
            };
            _session.InitializeAccount(_acc);

            var items = new List<Item>
            {
                new Item {Type = PocketType.Main, VNum = 1012, IsDroppable = true},
                new Item {Type = PocketType.Main, VNum = 1013},
                new Item {Type = PocketType.Equipment, VNum = 1, ItemType = ItemType.Weapon},
                new Item {Type = PocketType.Equipment, VNum = 912, ItemType = ItemType.Specialist},
                new Item {Type = PocketType.Equipment, VNum = 924, ItemType = ItemType.Fashion}
            };
            var conf = new WorldConfiguration { BackpackSize = 1, MaxItemAmount = 999 };
            _itemBuilder = new ItemBuilderService(items);
            _handler = new InventoryPacketController(conf);

            _handler.RegisterSession(_session);
            _session.SetCharacter(_chara.Adapt<Character>());
            _session.Character.MapInstance = _map;
            _session.Character.Inventory = new InventoryService(items, conf);
        }

        [TestMethod]
        public void Test_Delete_FromSlot()
        {
            _session.Character.Inventory.AddItemToPocket(_itemBuilder.Create(1012, 1, 999));
            _handler.AskToDelete(new BiPacket { Option = RequestDeletionType.Confirmed, Slot = 0, PocketType = PocketType.Main });
            var packet = (IvnPacket)_session.LastPacket;
            Assert.IsTrue(packet.IvnSubPackets.All(iv => iv.Slot == 0 && iv.VNum == -1));
        }

        [TestMethod]
        public void Test_Delete_FromEquiment()
        {
            _session.Character.Inventory.AddItemToPocket(_itemBuilder.Create(1, 1));
            _handler.AskToDelete(new BiPacket { Option = RequestDeletionType.Confirmed, Slot = 0, PocketType = PocketType.Equipment });
            Assert.IsTrue(_session.Character.Inventory.Count == 0);
            var packet = (IvnPacket)_session.LastPacket;
            Assert.IsTrue(packet.IvnSubPackets.All(iv => iv.Slot == 0 && iv.VNum == -1));
        }

        [TestMethod]
        public void Test_PutPartialSlot()
        {
            _session.Character.Inventory.AddItemToPocket(_itemBuilder.Create(1012, 1, 999));
            _handler.PutItem(new PutPacket
            {
                PocketType = PocketType.Main,
                Slot = 0,
                Amount = 500
            });
            Assert.IsTrue(_session.Character.Inventory.Count == 1 && _session.Character.Inventory.FirstOrDefault().Value.Amount == 499);
        }

        [TestMethod]
        public void Test_PutNotDroppable()
        {
            _session.Character.Inventory.AddItemToPocket(_itemBuilder.Create(1013, 1));
            _handler.PutItem(new PutPacket
            {
                PocketType = PocketType.Main,
                Slot = 0,
                Amount = 1
            });
            var packet = (MsgPacket)_session.LastPacket;
            Assert.IsTrue(packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.ITEM_NOT_DROPPABLE,
                _session.Account.Language) && packet.Type == 0);
            Assert.IsTrue(_session.Character.Inventory.Count > 0);
        }


        [TestMethod]
        public void Test_Put()
        {
            _session.Character.Inventory.AddItemToPocket(_itemBuilder.Create(1012, 1));

            _handler.PutItem(new PutPacket
            {
                PocketType = PocketType.Main,
                Slot = 0,
                Amount = 1
            });
            Assert.IsTrue(_session.Character.Inventory.Count == 0);
        }

        [TestMethod]
        public void Test_PutBadPlace()
        {
            _session.Character.PositionX = -13;
            _session.Character.PositionY = -13;
            _session.Character.Inventory.AddItemToPocket(_itemBuilder.Create(1012, 1));
            _handler.PutItem(new PutPacket
            {
                PocketType = PocketType.Main,
                Slot = 0,
                Amount = 1
            });
            var packet = (MsgPacket)_session.LastPacket;
            Assert.IsTrue(packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.ITEM_NOT_DROPPABLE_HERE,
                _session.Account.Language) && packet.Type == 0);
            Assert.IsTrue(_session.Character.Inventory.Count > 0);
        }

        [TestMethod]
        public void Test_Get()
        {
            _session.Character.PositionX = 0;
            _session.Character.PositionY = 0;
            _map.DroppedList.TryAdd(100001,
                new MapItem
                {
                    PositionX = 1,
                    PositionY = 1,
                    VisualId = 1012,
                    ItemInstance = _itemBuilder.Create(1012, 1),
                    MapInstanceId = _map.MapInstanceId,
                    MapInstance = _map
                });

            _handler.GetItem(new GetPacket
            {
                PickerId = _chara.CharacterId,
                VisualId = 100001,
                PickerType = PickerType.Character
            });
            Assert.IsTrue(_session.Character.Inventory.Count > 0);
        }

        [TestMethod]
        public void Test_Get_KeepRarity()
        {
            _session.Character.PositionX = 0;
            _session.Character.PositionY = 0;
            _map.DroppedList.TryAdd(100001,
                new MapItem
                {
                    PositionX = 1,
                    PositionY = 1,
                    VisualId = 1,
                    ItemInstance = _itemBuilder.Create(1, 1, 1, 6),
                    MapInstanceId = _map.MapInstanceId,
                    MapInstance = _map
                });

            _handler.GetItem(new GetPacket
            {
                PickerId = _chara.CharacterId,
                VisualId = 100001,
                PickerType = PickerType.Character
            });
            Assert.IsTrue(_session.Character.Inventory.First().Value.Rare == 6);
        }

        [TestMethod]
        public void Test_Get_NotYourObject()
        {
            _session.Character.PositionX = 0;
            _session.Character.PositionY = 0;
            _map.DroppedList.TryAdd(100001,
                new MapItem
                {
                    PositionX = 1,
                    PositionY = 1,
                    VisualId = 1012,
                    OwnerId = 2,
                    DroppedAt = DateTime.Now,
                    ItemInstance = _itemBuilder.Create(1012, 1),
                    MapInstanceId = _map.MapInstanceId,
                    MapInstance = _map
                });

            _handler.GetItem(new GetPacket
            {
                PickerId = _chara.CharacterId,
                VisualId = 100001,
                PickerType = PickerType.Character
            });
            var packet = (SayPacket)_session.LastPacket;
            Assert.IsTrue(packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.NOT_YOUR_ITEM,
                _session.Account.Language) && packet.Type == SayColorType.Yellow);
            Assert.IsTrue(_session.Character.Inventory.Count == 0);
        }

        [TestMethod]
        public void Test_Get_NotYourObjectAfterDelay()
        {
            _session.Character.PositionX = 0;
            _session.Character.PositionY = 0;
            _map.DroppedList.TryAdd(100001,
                new MapItem
                {
                    OwnerId = 2,
                    DroppedAt = DateTime.Now.AddSeconds(-30),
                    PositionX = 1,
                    PositionY = 1,
                    VisualId = 1012,
                    ItemInstance = _itemBuilder.Create(1012, 1),
                    MapInstanceId = _map.MapInstanceId,
                    MapInstance = _map
                });

            _handler.GetItem(new GetPacket
            {
                PickerId = _chara.CharacterId,
                VisualId = 100001,
                PickerType = PickerType.Character
            });
            Assert.IsTrue(_session.Character.Inventory.Count > 0);
        }

        [TestMethod]
        public void Test_GetAway()
        {
            _session.Character.PositionX = 8;
            _session.Character.PositionY = 8;
            _map.DroppedList.TryAdd(100001,
                new MapItem
                {
                    PositionX = 1,
                    PositionY = 1,
                    VisualId = 1012,
                    ItemInstance = _itemBuilder.Create(1012, 1),
                    MapInstanceId = _map.MapInstanceId,
                    MapInstance = _map
                });

            _handler.GetItem(new GetPacket
            {
                PickerId = _chara.CharacterId,
                VisualId = 100001,
                PickerType = PickerType.Character
            });
            Assert.IsTrue(_session.Character.Inventory.Count == 0);
        }

        [TestMethod]
        public void Test_GetFullInventory()
        {
            _session.Character.PositionX = 0;
            _session.Character.PositionY = 0;
            _map.DroppedList.TryAdd(100001,
                new MapItem
                {
                    PositionX = 1,
                    PositionY = 1,
                    VisualId = 1,
                    ItemInstance = _itemBuilder.Create(1, 1),
                    MapInstanceId = _map.MapInstanceId,
                    MapInstance = _map
                });
            _session.Character.Inventory.AddItemToPocket(_itemBuilder.Create(1, 1));
            _handler.GetItem(new GetPacket
            {
                PickerId = _chara.CharacterId,
                VisualId = 100001,
                PickerType = PickerType.Character
            });
            var packet = (MsgPacket)_session.LastPacket;
            Assert.IsTrue(packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_PLACE,
                _session.Account.Language) && packet.Type == 0);
            Assert.IsTrue(_session.Character.Inventory.Count == 1);
        }

        [TestMethod]
        public void Test_GetInStack()
        {
            _session.Character.PositionX = 0;
            _session.Character.PositionY = 0;
            _map.DroppedList.TryAdd(100001,
                new MapItem
                {
                    PositionX = 1,
                    PositionY = 1,
                    VisualId = 1012,
                    ItemInstance = _itemBuilder.Create(1012, 1),
                    MapInstanceId = _map.MapInstanceId,
                    MapInstance = _map
                });
            _session.Character.Inventory.AddItemToPocket(_itemBuilder.Create(1012, 1));
            _handler.GetItem(new GetPacket
            {
                PickerId = _chara.CharacterId,
                VisualId = 100001,
                PickerType = PickerType.Character
            });
            Assert.IsTrue(_session.Character.Inventory.First().Value.Amount == 2);
        }
    }
}