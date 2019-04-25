using System;
using System.Collections.Generic;
using System.Linq;
using ChickenAPI.Packets.ClientPackets.Drops;
using ChickenAPI.Packets.ClientPackets.Inventory;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.Chats;
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
using Serilog;
using Character = NosCore.GameObject.Character;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class WearPacketHandlerTests
    {
        private WearPacketHandler _wearPacketHandler;
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        private readonly ClientSession _session = new ClientSession(
            new WorldConfiguration
            { BackpackSize = 2, MaxItemAmount = 999, MaxSpPoints = 10_000, MaxAdditionalSpPoints = 1_000_000 }, _logger, new[] { new UseItemPacketHandler() });

        private Character _chara;
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
            var contextBuilder =
                new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(
                    databaseName: Guid.NewGuid().ToString());
            DataAccessHelper.Instance.InitializeForTest(contextBuilder.Options);
            var _acc = new AccountDto { Name = "AccountTest", Password = "test".ToSha512() };

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
                new ExchangeProvider(null, null, _logger), null, null, null, null, null, _logger, null)
            {
                CharacterId = 1,
                Name = "TestExistingCharacter",
                Slot = 1,
                AccountId = _acc.AccountId,
                MapId = 1,
                State = CharacterState.Active
            };
            _session.InitializeAccount(_acc);

            _item = new ItemProvider(items, new List<IEventHandler<Item, Tuple<IItemInstance, UseItemPacket>>>
            {
                new SpRechargerEventHandler(_session.WorldConfiguration),
                new VehicleEventHandler(_logger),
                new WearEventHandler(_logger)
            });

            _mapItemProvider = new MapItemProvider(new List<IEventHandler<MapItem, Tuple<MapItem, GetPacket>>>
                {new DropEventHandler(), new SpChargerEventHandler(), new GoldDropEventHandler()});
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
            _session.SetCharacter(_chara);
            _session.Character.MapInstance = _map;
            _session.Character.Account = _acc;
            _wearPacketHandler = new WearPacketHandler();
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
                new List<IEventHandler<Item, Tuple<IItemInstance, UseItemPacket>>> { new WearEventHandler(_logger) });

            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            _wearPacketHandler.Execute(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session);
            Assert.IsTrue(_session.Character.Inventory.All(s =>
                s.Value.Slot == (short)type && s.Value.Type == PocketType.Wear));
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
                new List<IEventHandler<Item, Tuple<IItemInstance, UseItemPacket>>> { new WearEventHandler(_logger) });

            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            _wearPacketHandler.Execute(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session);
            Assert.IsTrue(_session.Character.Inventory.All(s => s.Value.Type == PocketType.Equipment));
            var packet = (SayPacket)_session.LastPacket;
            Assert.IsTrue(packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.BAD_EQUIPMENT,
                _session.Account.Language) && packet.Type == SayColorType.Yellow);

            foreach (var validClass in Enum.GetValues(typeof(CharacterClassType)).OfType<CharacterClassType>()
                .Where(s => s != classToTest).ToList())
            {
                _session.Character.Class = validClass;
                var item = _session.Character.Inventory.First();
                _wearPacketHandler.Execute(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session);
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
                new List<IEventHandler<Item, Tuple<IItemInstance, UseItemPacket>>> { new WearEventHandler(_logger) });

            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            _wearPacketHandler.Execute(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session);
            Assert.IsTrue(_session.Character.Inventory.All(s => s.Value.Type == PocketType.Equipment));
            var packet = (SayPacket)_session.LastPacket;
            Assert.IsTrue(packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.BAD_EQUIPMENT,
                _session.Account.Language) && packet.Type == SayColorType.Yellow);

            foreach (var validClass in Enum.GetValues(typeof(GenderType)).OfType<GenderType>()
                .Where(s => s != genderToTest).ToList())
            {
                _session.Character.Gender = validClass;
                var item = _session.Character.Inventory.First();
                _wearPacketHandler.Execute(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session);
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
                new List<IEventHandler<Item, Tuple<IItemInstance, UseItemPacket>>> { new WearEventHandler(_logger) });
            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            _wearPacketHandler.Execute(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session);
            Assert.IsTrue(_session.Character.Inventory.All(s => s.Value.Type == PocketType.Equipment));
            var packet = (SayPacket)_session.LastPacket;
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
                new List<IEventHandler<Item, Tuple<IItemInstance, UseItemPacket>>> { new WearEventHandler(_logger) });
            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            _wearPacketHandler.Execute(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session);
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
                new List<IEventHandler<Item, Tuple<IItemInstance, UseItemPacket>>> { new WearEventHandler(_logger) });
            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            _wearPacketHandler.Execute(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session);
            Assert.IsTrue(_session.Character.Inventory.All(s => s.Value.Type == PocketType.Equipment));
            var packet = (SayPacket)_session.LastPacket;
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
                new List<IEventHandler<Item, Tuple<IItemInstance, UseItemPacket>>> { new WearEventHandler(_logger) });
            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            _wearPacketHandler.Execute(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session);
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
                new List<IEventHandler<Item, Tuple<IItemInstance, UseItemPacket>>> { new WearEventHandler(_logger) });
            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            _wearPacketHandler.Execute(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session);
            Assert.IsTrue(_session.Character.Inventory.All(s => s.Value.Type == PocketType.Equipment));
            var packet = (SayPacket)_session.LastPacket;
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
                new List<IEventHandler<Item, Tuple<IItemInstance, UseItemPacket>>> { new WearEventHandler(_logger) });
            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            _wearPacketHandler.Execute(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session);
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
                new List<IEventHandler<Item, Tuple<IItemInstance, UseItemPacket>>> { new WearEventHandler(_logger) });
            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1, 1, -2));
            _wearPacketHandler.Execute(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session);

            Assert.IsTrue(_session.Character.Inventory.Any(s => s.Value.Type == PocketType.Equipment));
            var packet = (MsgPacket)_session.LastPacket;
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
                new List<IEventHandler<Item, Tuple<IItemInstance, UseItemPacket>>> { new WearEventHandler(_logger) });
            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            _session.Character.Inventory.AddItemToPocket(_item.Create(2, 1));
            _wearPacketHandler.Execute(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session);
            _session.Character.UseSp = true;
            _wearPacketHandler.Execute(new WearPacket { InventorySlot = 1, Type = PocketType.Equipment }, _session);
            Assert.IsTrue(_session.Character.Inventory.Any(s =>
                s.Value.ItemVNum == 2 && s.Value.Type == PocketType.Equipment));
            var packet = (SayPacket)_session.LastPacket;
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
                new List<IEventHandler<Item, Tuple<IItemInstance, UseItemPacket>>> { new WearEventHandler(_logger) });
            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            _session.Character.Inventory.AddItemToPocket(_item.Create(2, 1));
            _wearPacketHandler.Execute(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session);
            _session.Character.SpCooldown = 30;
            _wearPacketHandler.Execute(new WearPacket { InventorySlot = 1, Type = PocketType.Equipment }, _session);
            Assert.IsTrue(_session.Character.Inventory.Any(s =>
                s.Value.ItemVNum == 2 && s.Value.Type == PocketType.Equipment));
            var packet = (MsgPacket)_session.LastPacket;
            Assert.IsTrue(packet.Message == string.Format(Language.Instance.GetMessageFromKey(LanguageKey.SP_INLOADING, _session.Account.Language), 30));
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
                new List<IEventHandler<Item, Tuple<IItemInstance, UseItemPacket>>> { new WearEventHandler(_logger) });

            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            _session.Character.Inventory.AddItemToPocket(_item.Create(2, 1));
            _wearPacketHandler.Execute(new WearPacket { InventorySlot = 1, Type = PocketType.Equipment }, _session);
            _session.Character.UseSp = true;
            _wearPacketHandler.Execute(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session);
            Assert.IsTrue(_session.Character.Inventory.Any(s =>
                s.Value.ItemVNum == 1 && s.Value.Type == PocketType.Equipment));
            var packet = (MsgPacket)_session.LastPacket;
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
                new List<IEventHandler<Item, Tuple<IItemInstance, UseItemPacket>>> { new WearEventHandler(_logger) });

            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            _session.Character.Inventory.AddItemToPocket(_item.Create(2, 1));
            _wearPacketHandler.Execute(new WearPacket { InventorySlot = 1, Type = PocketType.Equipment }, _session);
            _session.Character.UseSp = true;
            _wearPacketHandler.Execute(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session);
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
                new List<IEventHandler<Item, Tuple<IItemInstance, UseItemPacket>>> { new WearEventHandler(_logger) });

            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            _session.Character.Inventory.AddItemToPocket(_item.Create(2, 1));
            _wearPacketHandler.Execute(new WearPacket { InventorySlot = 1, Type = PocketType.Equipment }, _session);
            _session.Character.UseSp = true;
            _wearPacketHandler.Execute(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session);
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
                new List<IEventHandler<Item, Tuple<IItemInstance, UseItemPacket>>> { new WearEventHandler(_logger) });

            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            _wearPacketHandler.Execute(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session);

            var packet = (QnaPacket)_session.LastPacket;
            Assert.IsTrue(packet.YesPacket is UseItemPacket yespacket
                && yespacket.Slot == 0
                && yespacket.Type == PocketType.Equipment
                && packet.Question == _session.GetMessageFromKey(LanguageKey.ASK_BIND));
            Assert.IsTrue(_session.Character.Inventory.Any(s =>
                s.Value.ItemVNum == 1 && s.Value.Type == PocketType.Equipment));
        }

    }
}
