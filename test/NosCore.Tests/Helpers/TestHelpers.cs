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
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.Database;
using NosCore.Database.DAL;
using NosCore.Database.Entities;
using NosCore.GameObject;
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
using NosCore.PacketHandlers.CharacterScreen;
using NosCore.PacketHandlers.Friend;
using NosCore.PacketHandlers.Inventory;
using Serilog;
using Character = NosCore.Database.Entities.Character;
using CharacterRelation = NosCore.Database.Entities.CharacterRelation;
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
        private readonly IGenericDao<PortalDto> _portalDao;
        private readonly IGenericDao<MapMonsterDto> _mapMonsterDao;
        private readonly IGenericDao<MapNpcDto> _mapNpcDao;
        private readonly IGenericDao<ShopDto> _shopDao;
        private readonly IGenericDao<ShopItemDto> _shopItemDao;
        private readonly IGenericDao<CharacterRelationDto> _characterRelationDao;
        private readonly ItemInstanceDao _itemInstanceDao;
        private readonly IWebApiAccess _webApiAccess;
        public IGenericDao<CharacterDto> CharacterDao { get; }
        public MapItemProvider MapItemProvider { get; set; }

        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private TestHelpers()
        {

            _webApiAccess = new Mock<IWebApiAccess>().Object;
            AccountDao = new GenericDao<Account, AccountDto>(_logger);
            _portalDao = new GenericDao<Portal, PortalDto>(_logger);
            _mapMonsterDao = new GenericDao<MapMonster, MapMonsterDto>(_logger);
            _mapNpcDao = new GenericDao<MapNpc, MapNpcDto>(_logger);
            _shopDao = new GenericDao<Shop, ShopDto>(_logger);
            _shopItemDao = new GenericDao<ShopItem, ShopItemDto>(_logger);
            _characterRelationDao = new GenericDao<CharacterRelation, CharacterRelationDto>(_logger);
            CharacterDao = new GenericDao<Character, CharacterDto>(_logger);
            _itemInstanceDao = new ItemInstanceDao(_logger);
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

            var mapShop = new Map
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

            var npc = new MapNpcDto();
            _mapNpcDao.InsertOrUpdate(ref npc);

            var instanceAccessService = new MapInstanceProvider(new List<MapDto> { map, mapShop },
                MapItemProvider,
                _mapNpcDao,
                _mapMonsterDao, _portalDao, new Adapter(), _logger);
            instanceAccessService.Initialize();
            return instanceAccessService;
        }

        public WorldConfiguration WorldConfiguration { get; } = new WorldConfiguration
        {
            BackpackSize = 2, MaxItemAmount = 999, MaxSpPoints = 10_000, MaxAdditionalSpPoints = 1_000_000, MaxGoldAmount = 999_999_999
        };
        public List<ItemDto> ItemList { get; } = new List<ItemDto>
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

        public MapInstanceProvider MapInstanceProvider { get; }

        public IItemProvider GenerateItemProvider()
        {
            return new ItemProvider(ItemList, new List<IEventHandler<Item, Tuple<IItemInstance, UseItemPacket>>>
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
            var session = new ClientSession(WorldConfiguration, MapInstanceProvider, null, _logger,
                new List<IPacketHandler> { new CharNewPacketHandler(CharacterDao),
                    new BlInsPackettHandler(_webApiAccess),
                    new UseItemPacketHandler(),
                    new FinsPacketHandler(_webApiAccess),
                    new SelectPacketHandler(new Adapter(), CharacterDao, _logger, null, MapInstanceProvider, _itemInstanceDao) }, _webApiAccess);
            session.SessionId = _lastId;
            var chara = new GameObject.Character(new InventoryService(ItemList, session.WorldConfiguration, _logger),
                new ExchangeProvider(null, WorldConfiguration, _logger), null, CharacterDao,null, AccountDao, _logger, null)
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
