using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChickenAPI.Packets.ClientPackets.Drops;
using ChickenAPI.Packets.ClientPackets.Inventory;
using ChickenAPI.Packets.ClientPackets.Login;
using ChickenAPI.Packets.ClientPackets.Movement;
using ChickenAPI.Packets.ClientPackets.Shops;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.Chats;
using ChickenAPI.Packets.ServerPackets.Login;
using ChickenAPI.Packets.ServerPackets.Shop;
using ChickenAPI.Packets.ServerPackets.UI;
using DotNetty.Transport.Channels;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Encryption;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.Database;
using NosCore.Database.DAL;
using NosCore.GameObject;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.ExchangeProvider;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.ItemProvider.Handlers;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.GameObject.Providers.MapInstanceProvider;
using NosCore.GameObject.Providers.MapItemProvider;
using NosCore.GameObject.Providers.MapItemProvider.Handlers;
using NosCore.PacketHandlers.Friend;
using NosCore.PacketHandlers.Game;
using NosCore.PacketHandlers.Inventory;
using NosCore.PacketHandlers.Login;
using NosCore.PacketHandlers.Shops;
using Serilog;
using Character = NosCore.GameObject.Character;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class UseItemPacketHandlerTests
    {
        private UseItemPacketHandler _useItemPacketHandler;
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
            _useItemPacketHandler = new UseItemPacketHandler();
        }

        [TestMethod]
        public void Test_Binding()
        {
            var items = new List<ItemDto>
            {
                new Item {Type = PocketType.Equipment, VNum = 1, RequireBinding = true},
            };
            _item = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<IItemInstance, UseItemPacket>>> { new WearEventHandler(_logger) });

            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            _useItemPacketHandler.Execute(new UseItemPacket { Slot = 0, Type = PocketType.Equipment, Mode = 1 }, _session);

            Assert.IsTrue(_session.Character.Inventory.Any(s =>
                s.Value.ItemVNum == 1 && s.Value.Type == PocketType.Wear &&
                s.Value.BoundCharacterId == _session.Character.VisualId));
        }

        [TestMethod]
        public void Test_Increment_SpAdditionPoints()
        {
            _session.Character.SpAdditionPoint = 0;
            _session.Character.Inventory.AddItemToPocket(_item.Create(1078, 1));
            var item = _session.Character.Inventory.First();
            _useItemPacketHandler.Execute(new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = 1,
                Type = item.Value.Type,
                Slot = item.Value.Slot,
                Mode = 0,
                Parameter = 0
            }, _session);
            Assert.IsTrue(_session.Character.SpAdditionPoint != 0 && !(_session.LastPacket is MsgPacket));
        }

        [TestMethod]
        public void Test_Overflow_SpAdditionPoints()
        {
            _session.Character.SpAdditionPoint = _session.WorldConfiguration.MaxAdditionalSpPoints;
            _session.Character.Inventory.AddItemToPocket(_item.Create(1078, 1));
            var item = _session.Character.Inventory.First();
            _useItemPacketHandler.Execute(new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = 1,
                Type = item.Value.Type,
                Slot = item.Value.Slot,
                Mode = 0,
                Parameter = 0
            }, _session);
            var packet = (MsgPacket)_session.LastPacket;
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
            _useItemPacketHandler.Execute(new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = 1,
                Type = item.Value.Type,
                Slot = item.Value.Slot,
                Mode = 0,
                Parameter = 0
            }, _session);
            Assert.IsTrue(_session.Character.SpAdditionPoint == _session.WorldConfiguration.MaxAdditionalSpPoints &&
                !(_session.LastPacket is MsgPacket));
        }

    }
}
