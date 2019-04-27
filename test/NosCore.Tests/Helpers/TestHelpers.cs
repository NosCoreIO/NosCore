using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChickenAPI.Packets.ClientPackets.Drops;
using ChickenAPI.Packets.ClientPackets.Inventory;
using ChickenAPI.Packets.Enumerations;
using DotNetty.Transport.Channels;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Moq;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Encryption;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
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
using Serilog;

namespace NosCore.Tests.Helpers
{
    public class TestHelpers
    {
        private int _lastId;
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
        public IGenericDao<CharacterDto> CharacterDao { get; }
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private TestHelpers()
        {
            AccountDao = new GenericDao<Database.Entities.Account, AccountDto>(_logger);
            _portalDao = new GenericDao<Database.Entities.Portal, PortalDto>(_logger);
            _mapMonsterDao = new GenericDao<Database.Entities.MapMonster, MapMonsterDto>(_logger);
            _mapNpcDao = new GenericDao<Database.Entities.MapNpc, MapNpcDto>(_logger);
            _shopDao = new GenericDao<Database.Entities.Shop, ShopDto>(_logger);
            _shopItemDao = new GenericDao<Database.Entities.ShopItem, ShopItemDto>(_logger);
            _characterRelationDao = new GenericDao<Database.Entities.CharacterRelation, CharacterRelationDto>(_logger);
            CharacterDao = new GenericDao<Database.Entities.Character, CharacterDto>(_logger);
            InitDatabase();
            MapInstanceProvider = GenerateMapInstanceProvider();
        }
        private MapInstanceProvider GenerateMapInstanceProvider()
        {
            var mapItemProvider = new MapItemProvider(new List<IEventHandler<MapItem, Tuple<MapItem, GetPacket>>>
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
                mapItemProvider,
                _mapNpcDao,
                _mapMonsterDao, _portalDao, new Adapter(), _logger);
            instanceAccessService.Initialize();
            return instanceAccessService;
        }

        public WorldConfiguration WorldConfiguration = new WorldConfiguration { BackpackSize = 2, MaxItemAmount = 999, MaxSpPoints = 10_000, MaxAdditionalSpPoints = 1_000_000 };
        public List<ItemDto> ItemList = new List<ItemDto>
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

        public MapInstanceProvider MapInstanceProvider;

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
            var session = new ClientSession(WorldConfiguration, _logger, new List<IPacketHandler>());
            session.SessionId = _lastId;
            var chara = new Character(new InventoryService(ItemList, session.WorldConfiguration, _logger),
                new ExchangeProvider(null, WorldConfiguration, _logger), null, _characterRelationDao, CharacterDao, null, AccountDao, _logger, null)
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
