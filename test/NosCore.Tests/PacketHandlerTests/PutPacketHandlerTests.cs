using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChickenAPI.Packets.ClientPackets.Drops;
using ChickenAPI.Packets.ClientPackets.Inventory;
using ChickenAPI.Packets.ClientPackets.Login;
using ChickenAPI.Packets.ClientPackets.Movement;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.Login;
using ChickenAPI.Packets.ServerPackets.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Encryption;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
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
using NosCore.PacketHandlers.Login;
using Serilog;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class PutPacketHandlerTests
    {
        private PutPacketHandler _putPacketHandler;
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        private readonly ClientSession _session = new ClientSession(
            new WorldConfiguration
            { BackpackSize = 2, MaxItemAmount = 999, MaxSpPoints = 10_000, MaxAdditionalSpPoints = 1_000_000 }, _logger, new List<IPacketHandler>());

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
            _putPacketHandler = new PutPacketHandler(_session.WorldConfiguration);
        }

        [TestMethod]
        public void Test_PutPartialSlot()
        {
            _session.Character.Inventory.AddItemToPocket(_item.Create(1012, 1, 999));
            _putPacketHandler.Execute(new PutPacket
            {
                PocketType = PocketType.Main,
                Slot = 0,
                Amount = 500
            }, _session);
            Assert.IsTrue(_session.Character.Inventory.Count == 1 &&
                _session.Character.Inventory.FirstOrDefault().Value.Amount == 499);
        }

        [TestMethod]
        public void Test_PutNotDroppable()
        {
            _session.Character.Inventory.AddItemToPocket(_item.Create(1013, 1));
            _putPacketHandler.Execute(new PutPacket
            {
                PocketType = PocketType.Main,
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
            _session.Character.Inventory.AddItemToPocket(_item.Create(1012, 1));
            _putPacketHandler.Execute(new PutPacket
            {
                PocketType = PocketType.Main,
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
            _session.Character.Inventory.AddItemToPocket(_item.Create(1012, 1));
            _putPacketHandler.Execute(new PutPacket
            {
                PocketType = PocketType.Main,
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
            _session.Character.Inventory.AddItemToPocket(_item.Create(1012, 1));
            _putPacketHandler.Execute(new PutPacket
            {
                PocketType = PocketType.Main,
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
