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
using NosCore.PacketHandlers.Login;
using NosCore.PacketHandlers.Shops;
using Serilog;
using Character = NosCore.GameObject.Character;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class SellPacketHandlerTests
    {
        private SellPacketHandler _sellPacketHandler;
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private readonly IGenericDao<MapDto> _mapDao = new GenericDao<Database.Entities.Map, MapDto>(_logger);
        private readonly IGenericDao<AccountDto> _accountDao = new GenericDao<Database.Entities.Account, AccountDto>(_logger);
        private readonly IGenericDao<CharacterDto> _characterDao = new GenericDao<Database.Entities.Character, CharacterDto>(_logger);
        private readonly IGenericDao<CharacterRelationDto> _characterRelationDao = new GenericDao<Database.Entities.CharacterRelation, CharacterRelationDto>(_logger);
        private readonly IGenericDao<PortalDto> _portalDao = new GenericDao<Database.Entities.Portal, PortalDto>(_logger);
        private readonly IGenericDao<MapMonsterDto> _mapMonsterDao = new GenericDao<Database.Entities.MapMonster, MapMonsterDto>(_logger);
        private readonly IGenericDao<MapNpcDto> _mapNpcDao = new GenericDao<Database.Entities.MapNpc, MapNpcDto>(_logger);
        private readonly IGenericDao<IItemInstanceDto> _itemInstanceDao = new ItemInstanceDao(_logger);
        private readonly Map _map = new Map
        {
            MapId = 0,
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
        };

        private readonly Map _mapShop = new Map
        {
            MapId = 1,
            Name = "shopMap",
            ShopAllowed = true,
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
        };

        MapInstanceProvider _instanceProvider;
        private ClientSession _session;

        [TestInitialize]
        public void Setup()
        {
            TypeAdapterConfig.GlobalSettings.ForDestinationType<IInitializable>().AfterMapping(dest => Task.Run(() => dest.Initialize()));
            Broadcaster.Reset();
            var contextBuilder =
                new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(
                    databaseName: Guid.NewGuid().ToString());
            DataAccessHelper.Instance.InitializeForTest(contextBuilder.Options);
            var map = new MapDto { MapId = 1 };
            _mapDao.InsertOrUpdate(ref map);
            var account = new AccountDto { Name = "AccountTest", Password = "test".ToSha512() };
            _accountDao.InsertOrUpdate(ref account);
            WebApiAccess.RegisterBaseAdress();
            WebApiAccess.Instance.MockValues =
                new Dictionary<WebApiRoute, object>
                {
                    {WebApiRoute.Channel, new List<ChannelInfo> {new ChannelInfo()}},
                    {WebApiRoute.ConnectedAccount, new List<ConnectedAccount>()}
                };

            var conf = new WorldConfiguration { BackpackSize = 3, MaxItemAmount = 999, MaxGoldAmount = 999_999_999 };
            var _chara = new Character(new InventoryService(new List<ItemDto>(), conf, _logger), null, null, _characterRelationDao, _characterDao, _itemInstanceDao, _accountDao, _logger, null)
            {
                CharacterId = 1,
                Name = "TestExistingCharacter",
                Slot = 1,
                AccountId = account.AccountId,
                MapId = 1,
                State = CharacterState.Active
            };

            _instanceProvider = new MapInstanceProvider(new List<MapDto> { _map, _mapShop },
                new MapItemProvider(new List<IEventHandler<MapItem, Tuple<MapItem, GetPacket>>>()),
                _mapNpcDao,
                _mapMonsterDao, _portalDao, new Adapter(), _logger);
            _instanceProvider.Initialize();
            var channelMock = new Mock<IChannel>();
            _session = new ClientSession(null, _logger, null);
            _session.RegisterChannel(channelMock.Object);
            _session.InitializeAccount(account);
            _session.SessionId = 1;
            _session.SetCharacter(_chara);
            var mapinstance = _instanceProvider.GetBaseMapById(0);
            _session.Character.Account = account;
            _session.Character.MapInstance = mapinstance;
            _session.Character.MapInstance.Portals = new List<Portal>
            {
                new Portal
                {
                    DestinationMapId = _map.MapId,
                    Type = PortalType.Open,
                    SourceMapInstanceId = mapinstance.MapInstanceId,
                    DestinationMapInstanceId = mapinstance.MapInstanceId,
                    DestinationX = 5,
                    DestinationY = 5,
                    PortalId = 1,
                    SourceMapId = _map.MapId,
                    SourceX = 0,
                    SourceY = 0,
                }
            };

            Broadcaster.Instance.RegisterSession(_session);
            _sellPacketHandler = new SellPacketHandler(conf);
        }


        [TestMethod]
        public void UserCanNotSellInExchange()
        {
            _session.Character.InExchangeOrTrade = true;
            var items = new List<ItemDto>
            {
                new Item {Type = PocketType.Etc, VNum = 1, IsTradable = true},
            };
            var itemBuilder = new ItemProvider(items, new List<IEventHandler<Item, Tuple<IItemInstance, UseItemPacket>>>());

            _session.Character.Inventory.AddItemToPocket(itemBuilder.Create(1, 1), PocketType.Etc, 0);
            _session.Character.Inventory.AddItemToPocket(itemBuilder.Create(1, 2), PocketType.Etc, 1);
            _session.Character.Inventory.AddItemToPocket(itemBuilder.Create(1, 3), PocketType.Etc, 2);

            _session.Character.MapInstance = _instanceProvider.GetBaseMapById(1);
            _sellPacketHandler.Execute(new SellPacket { Slot = 0, Amount = 1, Data = (short)PocketType.Etc }, _session);
            Assert.IsTrue(_session.Character.Gold == 0);
            Assert.IsNotNull(_session.Character.Inventory.LoadBySlotAndType<IItemInstance>(0, PocketType.Etc));
        }

        [TestMethod]
        public void UserCanNotSellNotSoldable()
        {
            var items = new List<ItemDto>
            {
                new Item {Type = PocketType.Etc, VNum = 1, IsSoldable = false},
            };
            var itemBuilder = new ItemProvider(items, new List<IEventHandler<Item, Tuple<IItemInstance, UseItemPacket>>>());

            _session.Character.Inventory.AddItemToPocket(itemBuilder.Create(1, 1), PocketType.Etc, 0);
            _session.Character.Inventory.AddItemToPocket(itemBuilder.Create(1, 2), PocketType.Etc, 1);
            _session.Character.Inventory.AddItemToPocket(itemBuilder.Create(1, 3), PocketType.Etc, 2);

            _session.Character.MapInstance = _instanceProvider.GetBaseMapById(1);
            _sellPacketHandler.Execute(new SellPacket { Slot = 0, Amount = 1, Data = (short)PocketType.Etc }, _session);
            var packet = (SMemoPacket)_session.LastPacket;
            Assert.IsTrue(packet.Message ==
                Language.Instance.GetMessageFromKey(LanguageKey.ITEM_NOT_SOLDABLE, _session.Account.Language));
            Assert.IsTrue(_session.Character.Gold == 0);
            Assert.IsNotNull(_session.Character.Inventory.LoadBySlotAndType<IItemInstance>(0, PocketType.Etc));
        }

        [TestMethod]
        public void UserCanSell()
        {
            var items = new List<ItemDto>
            {
                new Item {Type = PocketType.Etc, VNum = 1, IsSoldable = true, Price = 500000},
            };
            var itemBuilder = new ItemProvider(items, new List<IEventHandler<Item, Tuple<IItemInstance, UseItemPacket>>>());

            _session.Character.Inventory.AddItemToPocket(itemBuilder.Create(1, 1), PocketType.Etc, 0);
            _session.Character.Inventory.AddItemToPocket(itemBuilder.Create(1, 2), PocketType.Etc, 1);
            _session.Character.Inventory.AddItemToPocket(itemBuilder.Create(1, 3), PocketType.Etc, 2);

            _session.Character.MapInstance = _instanceProvider.GetBaseMapById(1);
            _sellPacketHandler.Execute(new SellPacket { Slot = 0, Amount = 1, Data = (short)PocketType.Etc }, _session);
            Assert.IsTrue(_session.Character.Gold > 0);
            Assert.IsNull(_session.Character.Inventory.LoadBySlotAndType<IItemInstance>(0, PocketType.Etc));
        }
    }
}
