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
using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using DotNetty.Transport.Channels;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Moq;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Encryption;
using NosCore.Core.HttpClients.ChannelHttpClients;
using NosCore.Core.HttpClients.ConnectedAccountHttpClients;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Dto;
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
using NosCore.GameObject.Providers.MapInstanceProvider.Handlers;
using NosCore.GameObject.Providers.MapItemProvider;
using NosCore.GameObject.Providers.MapItemProvider.Handlers;
using NosCore.GameObject.Providers.MinilandProvider;
using NosCore.PacketHandlers.CharacterScreen;
using NosCore.PacketHandlers.Friend;
using NosCore.PacketHandlers.Inventory;
using Serilog;
using Character = NosCore.Database.Entities.Character;
using InventoryItemInstance = NosCore.Database.Entities.InventoryItemInstance;
using Item = NosCore.GameObject.Providers.ItemProvider.Item.Item;
using Map = NosCore.GameObject.Map.Map;
using MapMonster = NosCore.Database.Entities.MapMonster;
using MapNpc = NosCore.Database.Entities.MapNpc;
using Miniland = NosCore.Database.Entities.Miniland;
using Portal = NosCore.Database.Entities.Portal;
using Shop = NosCore.Database.Entities.Shop;
using ShopItem = NosCore.Database.Entities.ShopItem;

namespace NosCore.Tests.Helpers
{
    public class TestHelpers
    {
        private static Lazy<TestHelpers> _lazy =
            new Lazy<TestHelpers>(() => new TestHelpers());

        private readonly IGenericDao<InventoryItemInstanceDto> _inventoryItemInstanceDao;
        private readonly ItemInstanceDao _itemInstanceDao;
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private readonly IGenericDao<MapMonsterDto> _mapMonsterDao;
        private readonly IGenericDao<MapNpcDto> _mapNpcDao;

        private readonly IGenericDao<PortalDto> _portalDao;
        private readonly IGenericDao<ShopDto> _shopDao;
        private readonly IGenericDao<ShopItemDto> _shopItemDao;
        private readonly IGenericDao<StaticBonusDto> _staticBonusDao;
        private int _lastId = 100;
        public Mock<IBlacklistHttpClient> BlacklistHttpClient = new Mock<IBlacklistHttpClient>();
        public Mock<IChannelHttpClient> ChannelHttpClient = new Mock<IChannelHttpClient>();
        public Mock<IConnectedAccountHttpClient> ConnectedAccountHttpClient = new Mock<IConnectedAccountHttpClient>();
        public Mock<IFriendHttpClient> FriendHttpClient = new Mock<IFriendHttpClient>();
        public Mock<IPacketHttpClient> PacketHttpClient = new Mock<IPacketHttpClient>();

        private TestHelpers()
        {
            BlacklistHttpClient.Setup(s => s.GetBlackListsAsync(It.IsAny<long>()))
                .ReturnsAsync(new List<CharacterRelationStatus>());
            FriendHttpClient.Setup(s => s.GetListFriendsAsync(It.IsAny<long>()))
                .ReturnsAsync(new List<CharacterRelationStatus>());
            AccountDao = new GenericDao<Account, AccountDto, long>(_logger);
            _portalDao = new GenericDao<Portal, PortalDto, int>(_logger);
            _mapMonsterDao = new GenericDao<MapMonster, MapMonsterDto, long>(_logger);
            _mapNpcDao = new GenericDao<MapNpc, MapNpcDto, long>(_logger);
            MinilandDao = new GenericDao<Miniland, MinilandDto, Guid>(_logger);
            _shopDao = new GenericDao<Shop, ShopDto, int>(_logger);
            _shopItemDao = new GenericDao<ShopItem, ShopItemDto, int>(_logger);
            CharacterDao = new GenericDao<Character, CharacterDto, long>(_logger);
            _itemInstanceDao = new ItemInstanceDao(_logger);
            _inventoryItemInstanceDao = new GenericDao<InventoryItemInstance, InventoryItemInstanceDto, Guid>(_logger);
            _staticBonusDao = new GenericDao<StaticBonus, StaticBonusDto, long>(_logger);
            InitDatabase();
            MapInstanceProvider = GenerateMapInstanceProvider();
        }

        public static TestHelpers Instance => _lazy.Value;

        public IGenericDao<AccountDto> AccountDao { get; }
        public IGenericDao<CharacterDto> CharacterDao { get; }
        public IGenericDao<MinilandDto> MinilandDao { get; }
        public MapItemProvider? MapItemProvider { get; set; }
        public Guid MinilandId { get; set; } = Guid.NewGuid();

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
            new Item
            {
                Type = NoscorePocketType.Equipment, VNum = 2, EquipmentSlot = EquipmentType.Fairy,
                Element = ElementType.Water
            },
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
            instanceAccessService.AddMapInstance(new MapInstance(miniland, MinilandId, false,
                MapInstanceType.NormalInstance, MapItemProvider, _logger, new List<IMapInstanceEventHandler>()));
            return instanceAccessService;
        }

        public IItemProvider GenerateItemProvider()
        {
            return new ItemProvider(ItemList,
                new List<IEventHandler<Item,
                    Tuple<GameObject.Providers.InventoryService.InventoryItemInstance, UseItemPacket>>>
                {
                    new SpRechargerEventHandler(WorldConfiguration),
                    new VehicleEventHandler(_logger),
                    new WearEventHandler(_logger)
                });
        }

        private void InitDatabase()
        {
            TypeAdapterConfig.GlobalSettings.AllowImplicitSourceInheritance = false;
            TypeAdapterConfig.GlobalSettings.ForDestinationType<IInitializable>()
                .AfterMapping(dest => Task.Run(dest.Initialize));
            TypeAdapterConfig.GlobalSettings.ForDestinationType<IPacket>().Ignore(s => s.ValidationResult!);
            TypeAdapterConfig<MapNpcDto, GameObject.MapNpc>.NewConfig()
                .ConstructUsing(src => new GameObject.MapNpc(GenerateItemProvider(), _shopDao, _shopItemDao,
                    new List<NpcMonsterDto>(), _logger));
            TypeAdapterConfig<MapMonsterDto, GameObject.MapMonster>.NewConfig()
                .ConstructUsing(src => new GameObject.MapMonster(new List<NpcMonsterDto>(), _logger));
            var contextBuilder =
                new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(
                    Guid.NewGuid().ToString());
            DataAccessHelper.Instance.InitializeForTest(contextBuilder.Options);
        }

        public ClientSession GenerateSession()
        {
            _lastId++;
            var acc = new AccountDto
            { AccountId = _lastId, Name = "AccountTest" + _lastId, Password = "test".ToSha512() };
            AccountDao.InsertOrUpdate(ref acc);
            var minilandProvider = new Mock<IMinilandProvider>();
            var session = new ClientSession(WorldConfiguration, MapInstanceProvider, new Mock<IExchangeProvider>().Object, _logger,
                new List<IPacketHandler>
                {
                    new CharNewPacketHandler(CharacterDao, MinilandDao),
                    new BlInsPackettHandler(BlacklistHttpClient.Object),
                    new UseItemPacketHandler(),
                    new FinsPacketHandler(FriendHttpClient.Object, ChannelHttpClient.Object,
                        ConnectedAccountHttpClient.Object),
                    new SelectPacketHandler(CharacterDao, _logger, new Mock<IItemProvider>().Object, MapInstanceProvider,
                        _itemInstanceDao, _inventoryItemInstanceDao, _staticBonusDao, new Mock<IGenericDao<QuicklistEntryDto>>().Object, new Mock<IGenericDao<TitleDto>>().Object)
                }, FriendHttpClient.Object, new Mock<ISerializer>().Object, PacketHttpClient.Object, minilandProvider.Object)
            {
                SessionId = _lastId
            };

            var chara = new GameObject.Character(new InventoryService(ItemList, session.WorldConfiguration, _logger),
                new ExchangeProvider(new Mock<IItemProvider>().Object, WorldConfiguration, _logger), new Mock<IItemProvider>().Object, CharacterDao, new Mock<IGenericDao<IItemInstanceDto>>().Object, new Mock<IGenericDao<InventoryItemInstanceDto>>().Object, AccountDao,
                _logger, new Mock<IGenericDao<StaticBonusDto>>().Object, new Mock<IGenericDao<QuicklistEntryDto>>().Object, new Mock<IGenericDao<MinilandDto>>().Object, minilandProvider.Object, new Mock<IGenericDao<TitleDto>>().Object)
            {
                CharacterId = _lastId,
                Name = "TestExistingCharacter" + _lastId,
                Slot = 1,
                AccountId = acc.AccountId,
                MapId = 1,
                State = CharacterState.Active,
                Level = 1,
                JobLevel = 1,
                StaticBonusList = new List<StaticBonusDto>(),
                Titles = new List<TitleDto>()
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
            _lazy = new Lazy<TestHelpers>(() => new TestHelpers());
        }
    }
}