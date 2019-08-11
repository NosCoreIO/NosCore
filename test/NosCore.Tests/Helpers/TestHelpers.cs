using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChickenAPI.Packets.ClientPackets.Drops;
using ChickenAPI.Packets.ClientPackets.Inventory;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.Interfaces;
using DotNetty.Transport.Channels;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Moq;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Encryption;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Core.HttpClients.ConnectedAccountHttpClient;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.Database;
using NosCore.Database.DAL;
using NosCore.Database.Entities;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.HttpClients.BlacklistHttpClient;
using NosCore.GameObject.HttpClients.FriendHttpClient;
using NosCore.GameObject.HttpClients.PacketHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.ExchangeProvider;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.ItemProvider.Handlers;
using NosCore.GameObject.Providers.MapInstanceProvider;
using NosCore.GameObject.Providers.MapItemProvider;
using NosCore.GameObject.Providers.MapItemProvider.Handlers;
using NosCore.GameObject.Providers.MinilandProvider;
using NosCore.PacketHandlers.CharacterScreen;
using NosCore.PacketHandlers.Friend;
using NosCore.PacketHandlers.Inventory;
using Serilog;
using Character = NosCore.Database.Entities.Character;
using Item = NosCore.GameObject.Providers.ItemProvider.Item.Item;
using Map = NosCore.GameObject.Map.Map;
using MapMonster = NosCore.Database.Entities.MapMonster;
using MapNpc = NosCore.Database.Entities.MapNpc;
using Portal = NosCore.Database.Entities.Portal;
using Shop = NosCore.Database.Entities.Shop;
using ShopItem = NosCore.Database.Entities.ShopItem;

namespace NosCore.Tests.Helpers
{
    public class TestHelpers
    {
        private int _lastId = 100;
        private static Lazy<TestHelpers> lazy =
            new Lazy<TestHelpers>(() => new TestHelpers());
        public static TestHelpers Instance => lazy.Value;

        public IGenericDao<AccountDto> AccountDao { get; }
        public Mock<IBlacklistHttpClient> BlacklistHttpClient = new Mock<IBlacklistHttpClient>();
        public Mock<IChannelHttpClient> ChannelHttpClient = new Mock<IChannelHttpClient>();
        public Mock<IConnectedAccountHttpClient> ConnectedAccountHttpClient = new Mock<IConnectedAccountHttpClient>();
        public Mock<IFriendHttpClient> FriendHttpClient = new Mock<IFriendHttpClient>();
        public Mock<IPacketHttpClient> PacketHttpClient = new Mock<IPacketHttpClient>();

        private readonly IGenericDao<PortalDto> _portalDao;
        private readonly IGenericDao<MapMonsterDto> _mapMonsterDao;
        private readonly IGenericDao<MapNpcDto> _mapNpcDao;
        private readonly IGenericDao<ShopDto> _shopDao;
        private readonly IGenericDao<ShopItemDto> _shopItemDao;
        private readonly ItemInstanceDao _itemInstanceDao;
        private readonly IGenericDao<InventoryItemInstanceDto> _inventoryItemInstanceDao;
        private readonly IGenericDao<StaticBonusDto> _staticBonusDao;
        public IGenericDao<CharacterDto> CharacterDao { get; }
        public IGenericDao<MinilandDto> MinilandDao { get; }
        public MapItemProvider MapItemProvider { get; set; }
        public Guid MinilandId { get; set; } = Guid.NewGuid();
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private TestHelpers()
        {
            BlacklistHttpClient.Setup(s => s.GetBlackLists(It.IsAny<long>()))
                .Returns(new List<CharacterRelationStatus>());
            FriendHttpClient.Setup(s => s.GetListFriends(It.IsAny<long>()))
                .Returns(new List<CharacterRelationStatus>());
            AccountDao = new GenericDao<Account, AccountDto>(_logger);
            _portalDao = new GenericDao<Portal, PortalDto>(_logger);
            _mapMonsterDao = new GenericDao<MapMonster, MapMonsterDto>(_logger);
            _mapNpcDao = new GenericDao<MapNpc, MapNpcDto>(_logger);
            MinilandDao = new GenericDao<Database.Entities.Miniland, MinilandDto>(_logger);
            _shopDao = new GenericDao<Shop, ShopDto>(_logger);
            _shopItemDao = new GenericDao<ShopItem, ShopItemDto>(_logger);
            CharacterDao = new GenericDao<Character, CharacterDto>(_logger);
            _itemInstanceDao = new ItemInstanceDao(_logger);
            _inventoryItemInstanceDao = new GenericDao<Database.Entities.InventoryItemInstance, InventoryItemInstanceDto>(_logger);
            _staticBonusDao = new GenericDao<StaticBonus, StaticBonusDto>(_logger);
            InitDatabase();
            MapInstanceProvider = GenerateMapInstanceProvider();
        }
        private MapInstanceProvider GenerateMapInstanceProvider()
        {
            MapItemProvider = new MapItemProvider(new List<IEventHandler<MapItem, Tuple<MapItem, GetPacket>>>
                {new DropEventHandler(), new SpChargerEventHandler(), new GoldDropEventHandler()});
            var map = new Map
            {
                MapId = 0,
                NameI18NKey = "testMap",
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

            var mapShop = new Map
            {
                MapId = 1,
                NameI18NKey = "shopMap",
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

            var miniland = new Map
            {
                MapId = 20001,
                NameI18NKey = "miniland",
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
            var npc = new MapNpcDto();
            _mapNpcDao.InsertOrUpdate(ref npc);

            var instanceAccessService = new MapInstanceProvider(new List<MapDto> { map, mapShop, miniland },
                MapItemProvider,
                _mapNpcDao,
                _mapMonsterDao, _portalDao, _logger);
            instanceAccessService.Initialize();
            instanceAccessService.AddMapInstance(new MapInstance(miniland, MinilandId, false, MapInstanceType.NormalInstance, MapItemProvider, _logger));
            return instanceAccessService;
        }

        public WorldConfiguration WorldConfiguration { get; } = new WorldConfiguration
        {
            BackpackSize = 2,
            MaxItemAmount = 999,
            MaxSpPoints = 10_000,
            MaxAdditionalSpPoints = 1_000_000,
            MaxGoldAmount = 999_999_999
        };
        public List<ItemDto> ItemList { get; } = new List<ItemDto>
        {
            new Item {Type = NoscorePocketType.Main, VNum = 1012, IsDroppable = true},
            new Item {Type = NoscorePocketType.Main, VNum = 1013},
            new Item {Type = NoscorePocketType.Equipment, VNum = 1, ItemType = ItemType.Weapon},
            new Item {Type = NoscorePocketType.Equipment, VNum = 2, EquipmentSlot = EquipmentType.Fairy, Element = ElementType.Water},
            new Item
            {
                Type = NoscorePocketType.Equipment, VNum = 912, ItemType = ItemType.Specialist, ReputationMinimum = 2,
                Element = ElementType.Fire
            },
            new Item {Type = NoscorePocketType.Equipment, VNum = 924, ItemType = ItemType.Fashion},
            new Item
            {
                Type = NoscorePocketType.Main, VNum = 1078, ItemType = ItemType.Special,
                Effect = ItemEffectType.DroppedSpRecharger, EffectValue = 10_000, WaitDelay = 5_000
            }
        };

        public MapInstanceProvider MapInstanceProvider { get; }

        public IItemProvider GenerateItemProvider()
        {
            return new ItemProvider(ItemList, new List<IEventHandler<Item, Tuple<GameObject.Providers.InventoryService.InventoryItemInstance, UseItemPacket>>>
            {
                new SpRechargerEventHandler(WorldConfiguration),
                new VehicleEventHandler(_logger),
                new WearEventHandler(_logger)
            });
        }

        private void InitDatabase()
        {
            TypeAdapterConfig.GlobalSettings.AllowImplicitSourceInheritance = false;
            TypeAdapterConfig.GlobalSettings.ForDestinationType<IInitializable>().AfterMapping(dest => Task.Run(() => dest.Initialize()));
            TypeAdapterConfig.GlobalSettings.ForDestinationType<IPacket>().Ignore(s => s.ValidationResult);
            TypeAdapterConfig<MapNpcDto, GameObject.MapNpc>.NewConfig()
                .ConstructUsing(src => new GameObject.MapNpc(GenerateItemProvider(), _shopDao, _shopItemDao, new List<NpcMonsterDto>(), _logger));
            TypeAdapterConfig<MapMonsterDto, GameObject.MapMonster>.NewConfig()
                .ConstructUsing(src => new GameObject.MapMonster(new List<NpcMonsterDto>(), _logger));
            var contextBuilder =
                new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(
                    databaseName: Guid.NewGuid().ToString());
            DataAccessHelper.Instance.InitializeForTest(contextBuilder.Options);

        }

        public ClientSession GenerateSession()
        {
            _lastId++;
            var acc = new AccountDto { AccountId = _lastId, Name = "AccountTest" + _lastId, Password = "test".ToSha512() };
            AccountDao.InsertOrUpdate(ref acc);
            var minilandProvider = new Mock<IMinilandProvider>();
            var session = new ClientSession(WorldConfiguration, MapInstanceProvider, null, _logger,
                new List<IPacketHandler> { new CharNewPacketHandler(CharacterDao, MinilandDao),
                    new BlInsPackettHandler(BlacklistHttpClient.Object),
                    new UseItemPacketHandler(),
                    new FinsPacketHandler(FriendHttpClient.Object, ChannelHttpClient.Object, ConnectedAccountHttpClient.Object),
                    new SelectPacketHandler(new Adapter(), CharacterDao, _logger, null, MapInstanceProvider, _itemInstanceDao, _inventoryItemInstanceDao, _staticBonusDao, null) }, FriendHttpClient.Object, null, PacketHttpClient.Object, minilandProvider.Object)
            {
                SessionId = _lastId
            };
            var chara = new GameObject.Character(new InventoryService(ItemList, session.WorldConfiguration, _logger),
                new ExchangeProvider(null, WorldConfiguration, _logger), null, CharacterDao, null, null, AccountDao, _logger, null, null, null, null)
            {
                CharacterId = _lastId,
                Name = "TestExistingCharacter" + _lastId,
                Slot = 1,
                AccountId = acc.AccountId,
                MapId = 1,
                State = CharacterState.Active
            };
            var charaDto = chara.Adapt<CharacterDto>();
            CharacterDao.InsertOrUpdate(ref charaDto);
            session.InitializeAccount(acc);
            session.SetCharacter(chara);
            session.Character.MapInstance = MapInstanceProvider.GetBaseMapById(0);
            session.Character.Account = acc;
            session.RegisterChannel(new Mock<IChannel>().Object);
            Broadcaster.Instance.RegisterSession(session);
            return session;
        }

        public static void Reset()
        {
            lazy = new Lazy<TestHelpers>(() => new TestHelpers());
        }
    }
}
