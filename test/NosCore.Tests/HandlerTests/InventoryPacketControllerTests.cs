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

using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Configuration;
using NosCore.Controllers;
using NosCore.Core;
using NosCore.Core.Encryption;

using NosCore.Database;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.GameObject;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using ChickenAPI.Packets.ClientPackets;
using ChickenAPI.Packets.ServerPackets;
using System;
using System.Collections.Generic;
using System.Linq;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Map;
using NosCore.Database.DAL;
using NosCore.GameObject.Providers.ExchangeProvider;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.ItemProvider.Handlers;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.GameObject.Providers.MapInstanceProvider;
using NosCore.GameObject.Providers.MapItemProvider;
using NosCore.GameObject.Providers.MapItemProvider.Handlers;
using Serilog;
using ChickenAPI.Packets.Enumerations;

namespace NosCore.Tests.HandlerTests
{
    [TestClass]
    public class InventoryPacketControllerTests
    {
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        private readonly ClientSession _session = new ClientSession(
            new WorldConfiguration
                {BackpackSize = 2, MaxItemAmount = 999, MaxSpPoints = 10_000, MaxAdditionalSpPoints = 1_000_000},
            new List<PacketController> {new InventoryPacketController()}, null, null, _logger);

        private Character _chara;
        private InventoryPacketController _handler;
        private IItemProvider _item;
        private MapInstance _map;
        private MapItemProvider _mapItemProvider;

        [TestCleanup]
        public void Cleanup()
        {
            SystemTime.Freeze(DateTime.Now);
        }

        [TestInitialize]
        public void Setup()
        {
            SystemTime.Freeze();
            PacketFactory.Initialize<NoS0575Packet>();
            var contextBuilder =
                new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(
                    databaseName: Guid.NewGuid().ToString());
            DataAccessHelper.Instance.InitializeForTest(contextBuilder.Options);
            var _acc = new AccountDto {Name = "AccountTest", Password = "test".ToSha512()};

            var items = new List<ItemDto>
            {
                new Item {Type = PocketType.Main, VNum = 1012, IsDroppable = true},
                new Item {Type = PocketType.Main, VNum = 1013},
                new Item {Type = PocketType.Equipment, VNum = 1, ItemType = ItemType.Weapon},
                new Item {Type = PocketType.Equipment, VNum = 2, EquipmentSlot = EquipmentType.Fairy, Element = 2},
                new Item
                {
                    Type = PocketType.Equipment, VNum = 912, ItemType = ItemType.Specialist, ReputationMinimum = 2,
                    Element = 1
                },
                new Item {Type = PocketType.Equipment, VNum = 924, ItemType = ItemType.Fashion},
                new Item
                {
                    Type = PocketType.Main, VNum = 1078, ItemType = ItemType.Special,
                    Effect = ItemEffectType.DroppedSpRecharger, EffectValue = 10_000, WaitDelay = 5_000
                }
            };

            _chara = new Character(new InventoryService(items, _session.WorldConfiguration, _logger),
                new ExchangeProvider(null, null, _logger), null, null, null, null, null, _logger)
            {
                CharacterId = 1,
                Name = "TestExistingCharacter",
                Slot = 1,
                AccountId = _acc.AccountId,
                MapId = 1,
                State = CharacterState.Active
            };
            _session.InitializeAccount(_acc);

            _item = new ItemProvider(items, new List<IHandler<Item, Tuple<IItemInstance, UseItemPacket>>>
            {
                new SpRechargerHandler(_session.WorldConfiguration),
                new VehicleHandler(_logger),
                new WearHandler(_logger)
            });
            _handler = new InventoryPacketController(_session.WorldConfiguration, _logger);
            _mapItemProvider = new MapItemProvider(new List<IHandler<MapItem, Tuple<MapItem, GetPacket>>>
                {new DropHandler(), new SpChargerHandler(), new GoldDropHandler()});
            _map = new MapInstance(new Map
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
                        0, 0, 0, 0, 0, 0, 0, 0,
                        0, 0, 0, 0, 0, 0, 0, 0,
                        0, 0, 0, 0, 0, 0, 0, 0
                    }
            }
                , Guid.NewGuid(), false, MapInstanceType.BaseMapInstance,
                _mapItemProvider,
                null, _logger);
            _handler.RegisterSession(_session);
            _session.SetCharacter(_chara);
            _session.Character.MapInstance = _map;
            _session.Character.Account = _acc;
        }

        [TestMethod]
        public void Test_Delete_FromSlot()
        {
            _session.Character.Inventory.AddItemToPocket(_item.Create(1012, 1, 999));
            _handler.AskToDelete(new BiPacket
                {Option = RequestDeletionType.Confirmed, Slot = 0, PocketType = PocketType.Main});
            var packet = (IvnPacket) _session.LastPacket;
            Assert.IsTrue(packet.IvnSubPackets.All(iv => iv.Slot == 0 && iv.VNum == -1));
        }

        [TestMethod]
        public void Test_Delete_FromEquiment()
        {
            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            _handler.AskToDelete(new BiPacket
                {Option = RequestDeletionType.Confirmed, Slot = 0, PocketType = PocketType.Equipment});
            Assert.IsTrue(_session.Character.Inventory.Count == 0);
            var packet = (IvnPacket) _session.LastPacket;
            Assert.IsTrue(packet.IvnSubPackets.All(iv => iv.Slot == 0 && iv.VNum == -1));
        }

        [TestMethod]
        public void Test_PutPartialSlot()
        {
            _session.Character.Inventory.AddItemToPocket(_item.Create(1012, 1, 999));
            _handler.PutItem(new PutPacket
            {
                PocketType = PocketType.Main,
                Slot = 0,
                Amount = 500
            });
            Assert.IsTrue(_session.Character.Inventory.Count == 1 &&
                _session.Character.Inventory.FirstOrDefault().Value.Amount == 499);
        }

        [TestMethod]
        public void Test_PutNotDroppable()
        {
            _session.Character.Inventory.AddItemToPocket(_item.Create(1013, 1));
            _handler.PutItem(new PutPacket
            {
                PocketType = PocketType.Main,
                Slot = 0,
                Amount = 1
            });
            var packet = (MsgPacket) _session.LastPacket;
            Assert.IsTrue(packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.ITEM_NOT_DROPPABLE,
                _session.Account.Language) && packet.Type == 0);
            Assert.IsTrue(_session.Character.Inventory.Count > 0);
        }


        [TestMethod]
        public void Test_Put()
        {
            _session.Character.Inventory.AddItemToPocket(_item.Create(1012, 1));

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
            _session.Character.PositionX = 2;
            _session.Character.PositionY = 2;
            _session.Character.Inventory.AddItemToPocket(_item.Create(1012, 1));
            _handler.PutItem(new PutPacket
            {
                PocketType = PocketType.Main,
                Slot = 0,
                Amount = 1
            });
            var packet = (MsgPacket) _session.LastPacket;
            Assert.IsTrue(packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.ITEM_NOT_DROPPABLE_HERE,
                _session.Account.Language) && packet.Type == 0);
            Assert.IsTrue(_session.Character.Inventory.Count > 0);
        }

        [TestMethod]
        public void Test_PutOutOfBounds()
        {
            _session.Character.PositionX = -1;
            _session.Character.PositionY = -1;
            _session.Character.Inventory.AddItemToPocket(_item.Create(1012, 1));
            _handler.PutItem(new PutPacket
            {
                PocketType = PocketType.Main,
                Slot = 0,
                Amount = 1
            });
            var packet = (MsgPacket) _session.LastPacket;
            Assert.IsTrue(packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.ITEM_NOT_DROPPABLE_HERE,
                _session.Account.Language) && packet.Type == 0);
            Assert.IsTrue(_session.Character.Inventory.Count > 0);
        }

        [TestMethod]
        public void Test_Get()
        {
            _session.Character.PositionX = 0;
            _session.Character.PositionY = 0;
            _map.MapItems.TryAdd(100001, _mapItemProvider.Create(_map, _item.Create(1012, 1), 1, 1));

            _handler.GetItem(new GetPacket
            {
                PickerId = _chara.CharacterId,
                VisualId = 100001,
                PickerType = VisualType.Player
            });
            Assert.IsTrue(_session.Character.Inventory.Count > 0);
        }

        [TestMethod]
        public void Test_Get_KeepRarity()
        {
            _session.Character.PositionX = 0;
            _session.Character.PositionY = 0;
            _map.MapItems.TryAdd(100001, _mapItemProvider.Create(_map, _item.Create(1, 1, 1, 6), 1, 1));

            _handler.GetItem(new GetPacket
            {
                PickerId = _chara.CharacterId,
                VisualId = 100001,
                PickerType = VisualType.Player
            });
            Assert.IsTrue(_session.Character.Inventory.First().Value.Rare == 6);
        }

        [TestMethod]
        public void Test_Get_NotYourObject()
        {
            _session.Character.PositionX = 0;
            _session.Character.PositionY = 0;
            var mapItem = _mapItemProvider.Create(_map, _item.Create(1012, 1), 1, 1);
            mapItem.VisualId = 1012;
            mapItem.OwnerId = 2;
            mapItem.DroppedAt = SystemTime.Now();
            _map.MapItems.TryAdd(100001, mapItem);

            _handler.GetItem(new GetPacket
            {
                PickerId = _chara.CharacterId,
                VisualId = 100001,
                PickerType = VisualType.Player
            });
            var packet = (SayPacket) _session.LastPacket;
            Assert.IsTrue(packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.NOT_YOUR_ITEM,
                _session.Account.Language) && packet.Type == SayColorType.Yellow);
            Assert.IsTrue(_session.Character.Inventory.Count == 0);
        }

        [TestMethod]
        public void Test_Get_NotYourObjectAfterDelay()
        {
            _session.Character.PositionX = 0;
            _session.Character.PositionY = 0;

            var mapItem = _mapItemProvider.Create(_map, _item.Create(1012, 1), 1, 1);
            mapItem.VisualId = 1012;
            mapItem.OwnerId = 2;
            mapItem.DroppedAt = SystemTime.Now().AddSeconds(-30);
            _map.MapItems.TryAdd(100001, mapItem);

            _handler.GetItem(new GetPacket
            {
                PickerId = _chara.CharacterId,
                VisualId = 100001,
                PickerType = VisualType.Player
            });
            Assert.IsTrue(_session.Character.Inventory.Count > 0);
        }

        [TestMethod]
        public void Test_GetAway()
        {
            _session.Character.PositionX = 7;
            _session.Character.PositionY = 7;

            _map.MapItems.TryAdd(100001, _mapItemProvider.Create(_map, _item.Create(1012, 1), 1, 1));
            _handler.GetItem(new GetPacket
            {
                PickerId = _chara.CharacterId,
                VisualId = 100001,
                PickerType = VisualType.Player
            });
            Assert.IsTrue(_session.Character.Inventory.Count == 0);
        }

        [TestMethod]
        public void Test_GetFullInventory()
        {
            _session.Character.PositionX = 0;
            _session.Character.PositionY = 0;
            _map.MapItems.TryAdd(100001, _mapItemProvider.Create(_map, _item.Create(1, 1), 1, 1));
            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            _handler.GetItem(new GetPacket
            {
                PickerId = _chara.CharacterId,
                VisualId = 100001,
                PickerType = VisualType.Player
            });
            var packet = (MsgPacket) _session.LastPacket;
            Assert.IsTrue(packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_PLACE,
                _session.Account.Language) && packet.Type == 0);
            Assert.IsTrue(_session.Character.Inventory.Count == 2);
        }

        [DataTestMethod]
        [DataRow(EquipmentType.MainWeapon)]
        [DataRow(EquipmentType.Armor)]
        [DataRow(EquipmentType.Hat)]
        [DataRow(EquipmentType.Gloves)]
        [DataRow(EquipmentType.Boots)]
        [DataRow(EquipmentType.SecondaryWeapon)]
        [DataRow(EquipmentType.Necklace)]
        [DataRow(EquipmentType.Ring)]
        [DataRow(EquipmentType.Bracelet)]
        [DataRow(EquipmentType.Mask)]
        [DataRow(EquipmentType.Fairy)]
        [DataRow(EquipmentType.Amulet)]
        [DataRow(EquipmentType.Sp)]
        [DataRow(EquipmentType.CostumeSuit)]
        [DataRow(EquipmentType.CostumeHat)]
        [DataRow(EquipmentType.WeaponSkin)]
        public void Test_Wear_Put_Item_CorrectSlot(EquipmentType type)
        {
            var items = new List<ItemDto>
            {
                new Item
                {
                    Type = PocketType.Equipment, VNum = 1, EquipmentSlot = type,
                    Class = 31 //sum of all 2^class
                },
            };
            _item = new ItemProvider(items,
                new List<IHandler<Item, Tuple<IItemInstance, UseItemPacket>>> {new WearHandler(_logger) });

            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            _handler.Wear(new WearPacket {InventorySlot = 0, Type = PocketType.Equipment});
            Assert.IsTrue(_session.Character.Inventory.All(s =>
                s.Value.Slot == (short) type && s.Value.Type == PocketType.Wear));
        }

        [DataTestMethod]
        [DataRow(CharacterClassType.Adventurer)]
        [DataRow(CharacterClassType.Archer)]
        [DataRow(CharacterClassType.Magician)]
        [DataRow(CharacterClassType.MartialArtist)]
        [DataRow(CharacterClassType.Swordman)]
        public void Test_Wear_Put_Item_BadClass(CharacterClassType classToTest)
        {
            _session.Character.Class = classToTest;
            var items = new List<ItemDto>
            {
                new Item
                {
                    Type = PocketType.Equipment, VNum = 1, EquipmentSlot = EquipmentType.MainWeapon,
                    Class = (byte) (31 - Math.Pow(2, (byte) classToTest))
                },
            };
            _item = new ItemProvider(items,
                new List<IHandler<Item, Tuple<IItemInstance, UseItemPacket>>> {new WearHandler(_logger) });

            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            _handler.Wear(new WearPacket {InventorySlot = 0, Type = PocketType.Equipment});
            Assert.IsTrue(_session.Character.Inventory.All(s => s.Value.Type == PocketType.Equipment));
            var packet = (SayPacket) _session.LastPacket;
            Assert.IsTrue(packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.BAD_EQUIPMENT,
                _session.Account.Language) && packet.Type == SayColorType.Yellow);

            foreach (var validClass in Enum.GetValues(typeof(CharacterClassType)).OfType<CharacterClassType>()
                .Where(s => s != classToTest).ToList())
            {
                _session.Character.Class = validClass;
                var item = _session.Character.Inventory.First();
                _handler.Wear(new WearPacket {InventorySlot = 0, Type = PocketType.Equipment});
                Assert.IsTrue(item.Value.Type == PocketType.Wear);
                item.Value.Type = PocketType.Equipment;
                item.Value.Slot = 0;
            }
        }


        [DataTestMethod]
        [DataRow(GenderType.Female)]
        [DataRow(GenderType.Male)]
        public void Test_Wear_Put_Item_BadGender(GenderType genderToTest)
        {
            _session.Character.Gender = genderToTest;
            var items = new List<ItemDto>
            {
                new Item
                {
                    Type = PocketType.Equipment, VNum = 1, EquipmentSlot = EquipmentType.MainWeapon,
                    Sex = (byte) (3 - Math.Pow(2, (byte) genderToTest))
                },
            };
            _item = new ItemProvider(items,
                new List<IHandler<Item, Tuple<IItemInstance, UseItemPacket>>> {new WearHandler(_logger) });

            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            _handler.Wear(new WearPacket {InventorySlot = 0, Type = PocketType.Equipment});
            Assert.IsTrue(_session.Character.Inventory.All(s => s.Value.Type == PocketType.Equipment));
            var packet = (SayPacket) _session.LastPacket;
            Assert.IsTrue(packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.BAD_EQUIPMENT,
                _session.Account.Language) && packet.Type == SayColorType.Yellow);

            foreach (var validClass in Enum.GetValues(typeof(GenderType)).OfType<GenderType>()
                .Where(s => s != genderToTest).ToList())
            {
                _session.Character.Gender = validClass;
                var item = _session.Character.Inventory.First();
                _handler.Wear(new WearPacket {InventorySlot = 0, Type = PocketType.Equipment});
                Assert.IsTrue(item.Value.Type == PocketType.Wear);
                item.Value.Type = PocketType.Equipment;
                item.Value.Slot = 0;
            }
        }

        [TestMethod]
        public void Test_Wear_BadJobLevel()
        {
            _session.Character.JobLevel = 1;
            var items = new List<ItemDto>
            {
                new Item
                {
                    Type = PocketType.Equipment, VNum = 1,
                    EquipmentSlot = EquipmentType.MainWeapon,
                    LevelJobMinimum = 3
                },
            };
            _item = new ItemProvider(items,
                new List<IHandler<Item, Tuple<IItemInstance, UseItemPacket>>> {new WearHandler(_logger) });
            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            _handler.Wear(new WearPacket {InventorySlot = 0, Type = PocketType.Equipment});
            Assert.IsTrue(_session.Character.Inventory.All(s => s.Value.Type == PocketType.Equipment));
            var packet = (SayPacket) _session.LastPacket;
            Assert.IsTrue(packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.LOW_JOB_LVL,
                _session.Account.Language) && packet.Type == SayColorType.Yellow);
        }

        [TestMethod]
        public void Test_Wear_GoodJobLevel()
        {
            _session.Character.JobLevel = 3;
            var items = new List<ItemDto>
            {
                new Item
                {
                    Type = PocketType.Equipment, VNum = 1,
                    EquipmentSlot = EquipmentType.MainWeapon,
                    LevelJobMinimum = 3
                },
            };
            _item = new ItemProvider(items,
                new List<IHandler<Item, Tuple<IItemInstance, UseItemPacket>>> {new WearHandler(_logger) });
            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            _handler.Wear(new WearPacket {InventorySlot = 0, Type = PocketType.Equipment});
            Assert.IsTrue(_session.Character.Inventory.All(s => s.Value.Type == PocketType.Wear));
        }

        [TestMethod]
        public void Test_Wear_BadLevel()
        {
            _session.Character.Level = 1;
            var items = new List<ItemDto>
            {
                new Item
                {
                    Type = PocketType.Equipment, VNum = 1,
                    EquipmentSlot = EquipmentType.MainWeapon,
                    LevelMinimum = 3
                },
            };
            _item = new ItemProvider(items,
                new List<IHandler<Item, Tuple<IItemInstance, UseItemPacket>>> {new WearHandler(_logger) });
            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            _handler.Wear(new WearPacket {InventorySlot = 0, Type = PocketType.Equipment});
            Assert.IsTrue(_session.Character.Inventory.All(s => s.Value.Type == PocketType.Equipment));
            var packet = (SayPacket) _session.LastPacket;
            Assert.IsTrue(packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.BAD_EQUIPMENT,
                _session.Account.Language) && packet.Type == SayColorType.Yellow);
        }

        [TestMethod]
        public void Test_Wear_GoodLevel()
        {
            _session.Character.Level = 3;
            var items = new List<ItemDto>
            {
                new Item
                {
                    Type = PocketType.Equipment, VNum = 1,
                    EquipmentSlot = EquipmentType.MainWeapon,
                    LevelMinimum = 3
                },
            };
            _item = new ItemProvider(items,
                new List<IHandler<Item, Tuple<IItemInstance, UseItemPacket>>> {new WearHandler(_logger) });
            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            _handler.Wear(new WearPacket {InventorySlot = 0, Type = PocketType.Equipment});
            Assert.IsTrue(_session.Character.Inventory.All(s => s.Value.Type == PocketType.Wear));
        }

        [TestMethod]
        public void Test_Wear_BadHeroLevel()
        {
            _session.Character.HeroLevel = 1;
            var items = new List<ItemDto>
            {
                new Item
                {
                    Type = PocketType.Equipment, VNum = 1,
                    EquipmentSlot = EquipmentType.MainWeapon,
                    IsHeroic = true,
                    LevelMinimum = 3
                },
            };
            _item = new ItemProvider(items,
                new List<IHandler<Item, Tuple<IItemInstance, UseItemPacket>>> {new WearHandler(_logger) });
            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            _handler.Wear(new WearPacket {InventorySlot = 0, Type = PocketType.Equipment});
            Assert.IsTrue(_session.Character.Inventory.All(s => s.Value.Type == PocketType.Equipment));
            var packet = (SayPacket) _session.LastPacket;
            Assert.IsTrue(packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.BAD_EQUIPMENT,
                _session.Account.Language) && packet.Type == SayColorType.Yellow);
        }

        [TestMethod]
        public void Test_Wear_GoodHeroLevel()
        {
            _session.Character.HeroLevel = 3;
            var items = new List<ItemDto>
            {
                new Item
                {
                    Type = PocketType.Equipment, VNum = 1,
                    EquipmentSlot = EquipmentType.MainWeapon,
                    IsHeroic = true,
                    LevelMinimum = 3
                },
            };
            _item = new ItemProvider(items,
                new List<IHandler<Item, Tuple<IItemInstance, UseItemPacket>>> {new WearHandler(_logger) });
            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            _handler.Wear(new WearPacket {InventorySlot = 0, Type = PocketType.Equipment});
            Assert.IsTrue(_session.Character.Inventory.All(s => s.Value.Type == PocketType.Wear));
        }

        [TestMethod]
        public void Test_Wear_DestroyedSp()
        {
            _session.Character.HeroLevel = 1;
            var items = new List<ItemDto>
            {
                new Item
                {
                    Type = PocketType.Equipment, VNum = 1,
                    EquipmentSlot = EquipmentType.Sp
                },
            };
            _item = new ItemProvider(items,
                new List<IHandler<Item, Tuple<IItemInstance, UseItemPacket>>> {new WearHandler(_logger) });
            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1, 1, -2));
            _handler.Wear(new WearPacket {InventorySlot = 0, Type = PocketType.Equipment});

            Assert.IsTrue(_session.Character.Inventory.Any(s => s.Value.Type == PocketType.Equipment));
            var packet = (MsgPacket) _session.LastPacket;
            Assert.IsTrue(packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.CANT_EQUIP_DESTROYED_SP,
                _session.Account.Language));
        }

        [TestMethod]
        public void Test_Wear_SpInUse()
        {
            _session.Character.HeroLevel = 1;
            var items = new List<ItemDto>
            {
                new Item
                {
                    Type = PocketType.Equipment, VNum = 1,
                    EquipmentSlot = EquipmentType.Sp
                },
                new Item
                {
                    Type = PocketType.Equipment, VNum = 2,
                    EquipmentSlot = EquipmentType.Sp
                },
            };
            _item = new ItemProvider(items,
                new List<IHandler<Item, Tuple<IItemInstance, UseItemPacket>>> {new WearHandler(_logger) });
            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            _session.Character.Inventory.AddItemToPocket(_item.Create(2, 1));
            _handler.Wear(new WearPacket {InventorySlot = 0, Type = PocketType.Equipment});
            _session.Character.UseSp = true;
            _handler.Wear(new WearPacket {InventorySlot = 1, Type = PocketType.Equipment});
            Assert.IsTrue(_session.Character.Inventory.Any(s =>
                s.Value.ItemVNum == 2 && s.Value.Type == PocketType.Equipment));
            var packet = (SayPacket) _session.LastPacket;
            Assert.IsTrue(packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.SP_BLOCKED,
                _session.Account.Language) && packet.Type == SayColorType.Yellow);
        }

        [TestMethod]
        public void Test_Wear_SpInLoading()
        {
            _session.Character.HeroLevel = 1;
            var items = new List<ItemDto>
            {
                new Item
                {
                    Type = PocketType.Equipment, VNum = 1,
                    EquipmentSlot = EquipmentType.Sp
                },
                new Item
                {
                    Type = PocketType.Equipment, VNum = 2,
                    EquipmentSlot = EquipmentType.Sp
                },
            };
            _item = new ItemProvider(items,
                new List<IHandler<Item, Tuple<IItemInstance, UseItemPacket>>> {new WearHandler(_logger) });
            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            _session.Character.Inventory.AddItemToPocket(_item.Create(2, 1));
            _handler.Wear(new WearPacket {InventorySlot = 0, Type = PocketType.Equipment});
            _session.Character.SpCooldown = 30;
            _handler.Wear(new WearPacket {InventorySlot = 1, Type = PocketType.Equipment});
            Assert.IsTrue(_session.Character.Inventory.Any(s =>
                s.Value.ItemVNum == 2 && s.Value.Type == PocketType.Equipment));
            var packet = (MsgPacket) _session.LastPacket;
            Assert.IsTrue(packet.Message == string.Format(Language.Instance.GetMessageFromKey(LanguageKey.SP_INLOADING, _session.Account.Language), 30));
        }

        [TestMethod]
        public void Test_GetInStack()
        {
            _session.Character.PositionX = 0;
            _session.Character.PositionY = 0;

            _map.MapItems.TryAdd(100001, _mapItemProvider.Create(_map, _item.Create(1012, 1), 1, 1));
            _session.Character.Inventory.AddItemToPocket(_item.Create(1012, 1));
            _handler.GetItem(new GetPacket
            {
                PickerId = _chara.CharacterId,
                VisualId = 100001,
                PickerType = VisualType.Player
            });
            Assert.IsTrue(_session.Character.Inventory.First().Value.Amount == 2);
        }

        [TestMethod]
        public void Test_Wear_WearFairy_SpUseBadElement()
        {
            var items = new List<ItemDto>
            {
                new Item {Type = PocketType.Equipment, VNum = 1, EquipmentSlot = EquipmentType.Fairy, Element = 3},
                new Item
                {
                    Type = PocketType.Equipment, VNum = 2, EquipmentSlot = EquipmentType.Sp, Element = 1,
                    SecondaryElement = 2
                }
            };
            _item = new ItemProvider(items,
                new List<IHandler<Item, Tuple<IItemInstance, UseItemPacket>>> {new WearHandler(_logger) });

            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            _session.Character.Inventory.AddItemToPocket(_item.Create(2, 1));
            _handler.Wear(new WearPacket {InventorySlot = 1, Type = PocketType.Equipment});
            _session.Character.UseSp = true;
            _handler.Wear(new WearPacket {InventorySlot = 0, Type = PocketType.Equipment});
            Assert.IsTrue(_session.Character.Inventory.Any(s =>
                s.Value.ItemVNum == 1 && s.Value.Type == PocketType.Equipment));
            var packet = (MsgPacket) _session.LastPacket;
            Assert.IsTrue(packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.BAD_FAIRY,
                _session.Account.Language));
        }

        [TestMethod]
        public void Test_Wear_WearFairy_SpUseGoodElement()
        {
            var items = new List<ItemDto>
            {
                new Item {Type = PocketType.Equipment, VNum = 1, EquipmentSlot = EquipmentType.Fairy, Element = 1},
                new Item
                {
                    Type = PocketType.Equipment, VNum = 2, EquipmentSlot = EquipmentType.Sp, Element = 1,
                    SecondaryElement = 2
                }
            };
            _item = new ItemProvider(items,
                new List<IHandler<Item, Tuple<IItemInstance, UseItemPacket>>> {new WearHandler(_logger) });

            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            _session.Character.Inventory.AddItemToPocket(_item.Create(2, 1));
            _handler.Wear(new WearPacket {InventorySlot = 1, Type = PocketType.Equipment});
            _session.Character.UseSp = true;
            _handler.Wear(new WearPacket {InventorySlot = 0, Type = PocketType.Equipment});
            Assert.IsTrue(
                _session.Character.Inventory.Any(s => s.Value.ItemVNum == 1 && s.Value.Type == PocketType.Wear));
        }

        [TestMethod]
        public void Test_Wear_WearFairy_SpUseGoodSecondElement()
        {
            var items = new List<ItemDto>
            {
                new Item {Type = PocketType.Equipment, VNum = 1, EquipmentSlot = EquipmentType.Fairy, Element = 2},
                new Item
                {
                    Type = PocketType.Equipment, VNum = 2, EquipmentSlot = EquipmentType.Sp, Element = 1,
                    SecondaryElement = 2
                }
            };
            _item = new ItemProvider(items,
                new List<IHandler<Item, Tuple<IItemInstance, UseItemPacket>>> {new WearHandler(_logger) });

            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            _session.Character.Inventory.AddItemToPocket(_item.Create(2, 1));
            _handler.Wear(new WearPacket {InventorySlot = 1, Type = PocketType.Equipment});
            _session.Character.UseSp = true;
            _handler.Wear(new WearPacket {InventorySlot = 0, Type = PocketType.Equipment});
            Assert.IsTrue(
                _session.Character.Inventory.Any(s => s.Value.ItemVNum == 1 && s.Value.Type == PocketType.Wear));
        }

        [TestMethod]
        public void Test_Binding_Required()
        {
            var items = new List<ItemDto>
            {
                new Item {Type = PocketType.Equipment, VNum = 1, RequireBinding = true},
            };
            _item = new ItemProvider(items,
                new List<IHandler<Item, Tuple<IItemInstance, UseItemPacket>>> {new WearHandler(_logger) });

            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            _handler.Wear(new WearPacket {InventorySlot = 0, Type = PocketType.Equipment});

            var packet = (QnaPacket) _session.LastPacket;
            Assert.IsTrue(packet.YesPacket is UseItemPacket yespacket
                && yespacket.Slot == 0
                && yespacket.Type == PocketType.Equipment
                && packet.Question == _session.GetMessageFromKey(LanguageKey.ASK_BIND));
            Assert.IsTrue(_session.Character.Inventory.Any(s =>
                s.Value.ItemVNum == 1 && s.Value.Type == PocketType.Equipment));
        }

        [TestMethod]
        public void Test_Binding()
        {
            var items = new List<ItemDto>
            {
                new Item {Type = PocketType.Equipment, VNum = 1, RequireBinding = true},
            };
            _item = new ItemProvider(items,
                new List<IHandler<Item, Tuple<IItemInstance, UseItemPacket>>> {new WearHandler(_logger) });

            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            _handler.UseItem(new UseItemPacket {Slot = 0, Type = PocketType.Equipment, Mode = 1});

            Assert.IsTrue(_session.Character.Inventory.Any(s =>
                s.Value.ItemVNum == 1 && s.Value.Type == PocketType.Wear &&
                s.Value.BoundCharacterId == _session.Character.VisualId));
        }

        [TestMethod]
        public void Test_Transform_NoSp()
        {
            _handler.SpTransform(new SpTransformPacket {Type = 0});
            var packet = (MsgPacket) _session.LastPacket;
            Assert.IsTrue(packet.Message ==
                Language.Instance.GetMessageFromKey(LanguageKey.NO_SP, _session.Account.Language));
        }

        [TestMethod]
        public void Test_Transform_Vehicle()
        {
            _session.Character.IsVehicled = true;
            _session.Character.Inventory.AddItemToPocket(_item.Create(912, 1));
            var item = _session.Character.Inventory.First();
            item.Value.Type = PocketType.Wear;
            item.Value.Slot = (byte) EquipmentType.Sp;
            _handler.SpTransform(new SpTransformPacket {Type = 0});
            var packet = (MsgPacket) _session.LastPacket;
            Assert.IsTrue(packet.Message ==
                Language.Instance.GetMessageFromKey(LanguageKey.REMOVE_VEHICLE, _session.Account.Language));
        }


        [TestMethod]
        public void Test_Transform_Sitted()
        {
            _session.Character.IsSitting = true;
            _handler.SpTransform(new SpTransformPacket {Type = 0});
            Assert.IsNull(_session.LastPacket);
        }

        [TestMethod]
        public void Test_RemoveSp()
        {
            _session.Character.Inventory.AddItemToPocket(_item.Create(912, 1));
            var item = _session.Character.Inventory.First();
            _session.Character.UseSp = true;
            item.Value.Type = PocketType.Wear;
            item.Value.Slot = (byte) EquipmentType.Sp;
            _handler.SpTransform(new SpTransformPacket {Type = 1});
            Assert.IsFalse(_session.Character.UseSp);
        }

        [TestMethod]
        public void Test_Transform()
        {
            _session.Character.SpPoint = 1;
            _session.Character.Reput = 5000000;
            _session.Character.Inventory.AddItemToPocket(_item.Create(912, 1));
            var item = _session.Character.Inventory.First();
            item.Value.Type = PocketType.Wear;
            item.Value.Slot = (byte) EquipmentType.Sp;
            _handler.SpTransform(new SpTransformPacket {Type = 1});
            Assert.IsTrue(_session.Character.UseSp);
        }

        [TestMethod]
        public void Test_Transform_BadFairy()
        {
            _session.Character.SpPoint = 1;
            _session.Character.Reput = 5000000;
            var item = _session.Character.Inventory.AddItemToPocket(_item.Create(912, 1)).First();
            var fairy = _session.Character.Inventory.AddItemToPocket(_item.Create(2, 1)).First();

            item.Type = PocketType.Wear;
            item.Slot = (byte) EquipmentType.Sp;
            fairy.Type = PocketType.Wear;
            fairy.Slot = (byte) EquipmentType.Fairy;
            _handler.SpTransform(new SpTransformPacket {Type = 1});
            var packet = (MsgPacket) _session.LastPacket;
            Assert.IsTrue(packet.Message ==
                Language.Instance.GetMessageFromKey(LanguageKey.BAD_FAIRY, _session.Account.Language));
        }

        [TestMethod]
        public void Test_Transform_BadReput()
        {
            _session.Character.SpPoint = 1;
            _session.Character.Inventory.AddItemToPocket(_item.Create(912, 1));
            var item = _session.Character.Inventory.First();
            item.Value.Type = PocketType.Wear;
            item.Value.Slot = (byte) EquipmentType.Sp;
            _handler.SpTransform(new SpTransformPacket {Type = 1});
            var packet = (MsgPacket) _session.LastPacket;
            Assert.IsTrue(packet.Message ==
                Language.Instance.GetMessageFromKey(LanguageKey.LOW_REP, _session.Account.Language));
        }


        [TestMethod]
        public void Test_TransformBefore_Cooldown()
        {
            _session.Character.SpPoint = 1;
            _session.Character.LastSp = SystemTime.Now();
            _session.Character.SpCooldown = 30;
            _session.Character.Inventory.AddItemToPocket(_item.Create(912, 1));
            var item = _session.Character.Inventory.First();
            item.Value.Type = PocketType.Wear;
            item.Value.Slot = (byte) EquipmentType.Sp;
            _handler.SpTransform(new SpTransformPacket {Type = 1});
            var packet = (MsgPacket) _session.LastPacket;
            Assert.IsTrue(packet.Message ==
                string.Format(Language.Instance.GetMessageFromKey(LanguageKey.SP_INLOADING, _session.Account.Language),
                    30));
        }

        [TestMethod]
        public void Test_Transform_OutOfSpPoint()
        {
            _session.Character.LastSp = SystemTime.Now();
            _session.Character.Inventory.AddItemToPocket(_item.Create(912, 1));
            var item = _session.Character.Inventory.First();
            item.Value.Type = PocketType.Wear;
            item.Value.Slot = (byte) EquipmentType.Sp;
            _handler.SpTransform(new SpTransformPacket {Type = 1});
            var packet = (MsgPacket) _session.LastPacket;
            Assert.IsTrue(packet.Message ==
                Language.Instance.GetMessageFromKey(LanguageKey.SP_NOPOINTS, _session.Account.Language));
        }

        [TestMethod]
        public void Test_Transform_Delay()
        {
            _session.Character.SpPoint = 1;
            _session.Character.LastSp = SystemTime.Now();
            _session.Character.Inventory.AddItemToPocket(_item.Create(912, 1));
            var item = _session.Character.Inventory.First();
            item.Value.Type = PocketType.Wear;
            item.Value.Slot = (byte) EquipmentType.Sp;
            _handler.SpTransform(new SpTransformPacket {Type = 0});
            var packet = (DelayPacket) _session.LastPacket;
            Assert.IsTrue(packet.Delay == 5000);
        }

        [TestMethod]
        public void Test_Increment_SpAdditionPoints()
        {
            _session.Character.SpAdditionPoint = 0;
            _session.Character.Inventory.AddItemToPocket(_item.Create(1078, 1));
            var item = _session.Character.Inventory.First();
            _handler.UseItem(new UseItemPacket
            {
                VisualType = VisualType.Player, VisualId = 1, Type = item.Value.Type, Slot = item.Value.Slot, Mode = 0,
                Parameter = 0
            });
            Assert.IsTrue(_session.Character.SpAdditionPoint != 0 && !(_session.LastPacket is MsgPacket));
        }

        [TestMethod]
        public void Test_Overflow_SpAdditionPoints()
        {
            _session.Character.SpAdditionPoint = _session.WorldConfiguration.MaxAdditionalSpPoints;
            _session.Character.Inventory.AddItemToPocket(_item.Create(1078, 1));
            var item = _session.Character.Inventory.First();
            _handler.UseItem(new UseItemPacket
            {
                VisualType = VisualType.Player, VisualId = 1, Type = item.Value.Type, Slot = item.Value.Slot, Mode = 0,
                Parameter = 0
            });
            var packet = (MsgPacket) _session.LastPacket;
            Assert.IsTrue(_session.Character.SpAdditionPoint == _session.WorldConfiguration.MaxAdditionalSpPoints &&
                packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.SP_ADDPOINTS_FULL,
                    _session.Character.Account.Language));
        }

        [TestMethod]
        public void Test_CloseToLimit_SpAdditionPoints()
        {
            _session.Character.SpAdditionPoint = _session.WorldConfiguration.MaxAdditionalSpPoints - 1;
            _session.Character.Inventory.AddItemToPocket(_item.Create(1078, 1));
            var item = _session.Character.Inventory.First();
            _handler.UseItem(new UseItemPacket
            {
                VisualType = VisualType.Player, VisualId = 1, Type = item.Value.Type, Slot = item.Value.Slot, Mode = 0,
                Parameter = 0
            });
            Assert.IsTrue(_session.Character.SpAdditionPoint == _session.WorldConfiguration.MaxAdditionalSpPoints &&
                !(_session.LastPacket is MsgPacket));
        }
    }
}