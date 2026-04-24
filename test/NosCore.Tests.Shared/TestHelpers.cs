//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Moq;
using NodaTime;
using NodaTime.Testing;
using NosCore.Algorithm.DignityService;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.HpService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Algorithm.MpService;
using NosCore.Algorithm.ReputationService;
using NosCore.Core.Configuration;
using NosCore.Core.Encryption;
using NosCore.Core.I18N;
using NosCore.Core.Services.IdService;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.Database;
using NosCore.Database.Entities;
using NosCore.GameObject.Map;
using NosCore.GameObject.Services.MinilandService;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.BazaarHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.CharacterService;
using NosCore.GameObject.Services.ExchangeService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceAccessService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MapItemGenerationService;
using NosCore.GameObject.Services.SpeedCalculationService;
using NosCore.GameObject.Services.TransformationService;
using NosCore.GameObject.Services.QuestService;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.Networking;
using NosCore.Networking.Encoding;
using NosCore.Networking.SessionGroup;
using NosCore.Networking.SessionRef;
using NosCore.PacketHandlers.Bazaar;
using NosCore.PacketHandlers.CharacterScreen;
using NosCore.PacketHandlers.Friend;
using NosCore.PacketHandlers.Inventory;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.PathFinder.Heuristic;
using NosCore.PathFinder.Interfaces;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Character = NosCore.Database.Entities.Character;
using InventoryItemInstance = NosCore.Database.Entities.InventoryItemInstance;
using Item = NosCore.GameObject.Services.ItemGenerationService.Item.Item;
using ItemInstance = NosCore.Database.Entities.ItemInstance;
using Map = NosCore.GameObject.Map.Map;
using MapMonster = NosCore.Database.Entities.MapMonster;
using MapNpc = NosCore.Database.Entities.MapNpc;
using Miniland = NosCore.Database.Entities.Miniland;
using Portal = NosCore.Database.Entities.Portal;
using ShopItem = NosCore.Database.Entities.ShopItem;

namespace NosCore.Tests.Shared
{
    public class TestHelpers
    {
        private static Lazy<TestHelpers> Lazy = new(() => new TestHelpers());

        private IDao<InventoryItemInstanceDto, Guid> InventoryItemInstanceDao = null!;
        private IDao<IItemInstanceDto?, Guid> ItemInstanceDao = null!;
        private IDao<MapMonsterDto, int> MapMonsterDao = null!;
        private IDao<MapNpcDto, int> MapNpcDao = null!;
        private IDao<PortalDto, int> PortalDao = null!;
        private IDao<ShopItemDto, int> ShopItemDao = null!;
        private IDao<StaticBonusDto, long> StaticBonusDao = null!;
        private int LastId = 100;
        public Mock<IBlacklistHub> BlacklistHttpClient = new();
        public Mock<IChannelHub> ChannelHttpClient = new();
        public Mock<IPubSubHub> PubSubHub = new();
        public Mock<IFriendHub> FriendHttpClient = new();
        public NosCore.GameObject.Services.BroadcastService.SessionRegistry SessionRegistry = new(NullLogger<NosCore.GameObject.Services.BroadcastService.SessionRegistry>.Instance);
        public FakeClock Clock = new(Instant.FromUtc(2021, 01, 01, 01, 01, 01));
        public ISessionGroupFactory SessionGroupFactory { get; private set; } = null!;
        private TestHelpers()
        {
            var sessionGroupFactoryMock = new Mock<ISessionGroupFactory>();
            sessionGroupFactoryMock.Setup(x => x.Create()).Returns(new Mock<ISessionGroup>().Object);
            SessionGroupFactory = sessionGroupFactoryMock.Object;
            Broadcaster.Initialize(sessionGroupFactoryMock.Object);
            BlacklistHttpClient.Setup(s => s.GetBlacklistedAsync(It.IsAny<long>()))
                .ReturnsAsync(new List<CharacterRelationStatus>());
            FriendHttpClient.Setup(s => s.GetFriendsAsync(It.IsAny<long>()))
                .ReturnsAsync(new List<CharacterRelationStatus>());
            InitDatabase();
            var mock = new Mock<ILogLanguageLocalizer<LogLanguageKey>>();
            mock.Setup(x => x[It.IsAny<LogLanguageKey>()])
                .Returns((LogLanguageKey x) => new LocalizedString(x.ToString(), x.ToString(), false));
            LogLanguageLocalizer = mock.Object;

            var mock2 = new Mock<IGameLanguageLocalizer>();
            mock2.Setup(x => x[It.IsAny<LanguageKey>(), It.IsAny<RegionType>()])
                .Returns((LogLanguageKey x, RegionType reg) => new LocalizedString($"{x}{reg}", $"{x}{reg}", false));
            GameLanguageLocalizer = mock2.Object;
        }

        public static TestHelpers Instance => Lazy.Value;

        public IDao<AccountDto, long> AccountDao { get; private set; } = null!;
        public IDao<MateDto, long> MateDao { get; private set; } = null!;
        public IDao<CharacterRelationDto, Guid> CharacterRelationDao { get; set; } = null!;
        public IDao<CharacterDto, long> CharacterDao { get; private set; } = null!;
        public IDao<MinilandDto, Guid> MinilandDao { get; private set; } = null!;
        public IGameLanguageLocalizer GameLanguageLocalizer { get; private set; } = null!;
        public ILogLanguageLocalizer<LogLanguageKey> LogLanguageLocalizer { get; private set; } = null!;
        public IDao<MinilandObjectDto, Guid> MinilandObjectDao { get; private set; } = null!;
        public IMapItemGenerationService? MapItemProvider { get; set; }
        public Guid MinilandId { get; set; } = Guid.NewGuid();

        public IOptions<WorldConfiguration> WorldConfiguration { get; } = Options.Create(new WorldConfiguration
        {
            BackpackSize = 2,
            MaxItemAmount = 999,
            MaxSpPoints = 10_000,
            MaxAdditionalSpPoints = 1_000_000,
            MaxGoldAmount = 999_999_999
        });

        public List<ItemDto> ItemList { get; } = new()
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

        public MapInstanceGeneratorService MapInstanceGeneratorService { get; set; } = null!;

        public MapInstanceAccessorService MapInstanceAccessorService { get; set; } = null!;
        public MapChangeService MapChangeService { get; set; } = null!;
        public IHeuristic DistanceCalculator { get; set; } = new OctileDistanceHeuristic();
        public Mock<IChannelHub> ChannelHub = new Mock<IChannelHub>();

        private async Task GenerateMapInstanceProviderAsync()
        {
            MapItemProvider = new MapItemGenerationService(new IdService<GameObject.Ecs.MapItemComponentBundle>(1));
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
            await MapNpcDao.TryInsertOrUpdateAsync(npc);
            var mapInstanceRegistry = new MapInstanceRegistry();
            MapInstanceAccessorService = new MapInstanceAccessorService(mapInstanceRegistry);
            var minilandServiceMock = new Mock<IMinilandService>();
            minilandServiceMock.Setup(s => s.GetMinilandPortals(It.IsAny<long>())).Returns(new List<GameObject.Map.Portal>());
            MapChangeService = new MapChangeService(new Mock<IExperienceService>().Object, new Mock<IJobExperienceService>().Object, new Mock<IHeroExperienceService>().Object,
                MapInstanceAccessorService, Instance.Clock, Instance.LogLanguageLocalizer, minilandServiceMock.Object, NullLogger<MapChangeService>.Instance, Instance.LogLanguageLocalizer, Instance.GameLanguageLocalizer, SessionRegistry, new Mock<Wolverine.IMessageBus>().Object);
            var mapChangeService = MapChangeService;
            var instanceGeneratorService = new MapInstanceGeneratorService(new List<MapDto> { map, mapShop, miniland }, new List<NpcMonsterDto>(), new List<NpcTalkDto>(), new List<ShopDto>(),
                MapItemProvider,
                MapNpcDao,
                MapMonsterDao, PortalDao, ShopItemDao, NullLoggerFactory.Instance,
                mapInstanceRegistry, MapInstanceAccessorService, Instance.Clock, Instance.LogLanguageLocalizer, mapChangeService, SessionGroupFactory, SessionRegistry, GenerateItemProvider(), Instance.DistanceCalculator,
                new Mock<NosCore.GameObject.Services.BattleService.IMonsterAi>().Object,
                new Mock<NosCore.GameObject.Services.BattleService.IBuffService>().Object,
                new Mock<NosCore.GameObject.Services.BattleService.IRegenerationService>().Object,
                new Mock<NosCore.GameObject.Services.BattleService.IBattleService>().Object);
            await instanceGeneratorService.InitializeAsync();
            await instanceGeneratorService.AddMapInstanceAsync(new MapInstance(miniland, MinilandId, false,
                MapInstanceType.NormalInstance, MapItemProvider, NullLogger<MapInstance>.Instance, Clock, mapChangeService, SessionGroupFactory, SessionRegistry, Instance.DistanceCalculator));
            MapInstanceGeneratorService = instanceGeneratorService;
        }

        public IItemGenerationService GenerateItemProvider()
        {
            return new ItemGenerationService(ItemList, NullLoggerFactory.Instance, Instance.LogLanguageLocalizer);
        }

        public void InitDatabase()
        {
            var optionsBuilder = new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(
                Guid.NewGuid().ToString());
            DbContext ContextBuilder() => new NosCoreContext(optionsBuilder.Options);
            CharacterRelationDao = new Dao<Database.Entities.CharacterRelation, CharacterRelationDto, Guid>(NullLogger<Dao<Database.Entities.CharacterRelation, CharacterRelationDto, Guid>>.Instance, ContextBuilder);
            AccountDao = new Dao<Account, AccountDto, long>(NullLogger<Dao<Account, AccountDto, long>>.Instance, ContextBuilder);
            MateDao = new Dao<Mate, MateDto, long>(NullLogger<Dao<Mate, MateDto, long>>.Instance, ContextBuilder);
            PortalDao = new Dao<Portal, PortalDto, int>(NullLogger<Dao<Portal, PortalDto, int>>.Instance, ContextBuilder);
            MapMonsterDao = new Dao<MapMonster, MapMonsterDto, int>(NullLogger<Dao<MapMonster, MapMonsterDto, int>>.Instance, ContextBuilder);
            MapNpcDao = new Dao<MapNpc, MapNpcDto, int>(NullLogger<Dao<MapNpc, MapNpcDto, int>>.Instance, ContextBuilder);
            MinilandDao = new Dao<Miniland, MinilandDto, Guid>(NullLogger<Dao<Miniland, MinilandDto, Guid>>.Instance, ContextBuilder);
            MinilandObjectDao = new Dao<MinilandObject, MinilandObjectDto, Guid>(NullLogger<Dao<MinilandObject, MinilandObjectDto, Guid>>.Instance, ContextBuilder);
            ShopItemDao = new Dao<ShopItem, ShopItemDto, int>(NullLogger<Dao<ShopItem, ShopItemDto, int>>.Instance, ContextBuilder);
            CharacterDao = new Dao<Character, CharacterDto, long>(NullLogger<Dao<Character, CharacterDto, long>>.Instance, ContextBuilder);
            ItemInstanceDao = new Dao<ItemInstance, IItemInstanceDto?, Guid>(NullLogger<Dao<ItemInstance, IItemInstanceDto?, Guid>>.Instance, ContextBuilder);
            InventoryItemInstanceDao = new Dao<InventoryItemInstance, InventoryItemInstanceDto, Guid>(NullLogger<Dao<InventoryItemInstance, InventoryItemInstanceDto, Guid>>.Instance, ContextBuilder);
            StaticBonusDao = new Dao<StaticBonus, StaticBonusDto, long>(NullLogger<Dao<StaticBonus, StaticBonusDto, long>>.Instance, ContextBuilder);
            TypeAdapterConfig.GlobalSettings.AllowImplicitSourceInheritance = false;
            TypeAdapterConfig.GlobalSettings.ForDestinationType<IPacket>().Ignore(s => s.ValidationResult);
        }

        public async Task<ClientSession> GenerateSessionAsync(List<IPacketHandler>? packetHandlers = null)
        {
            LastId++;
            var acc = new AccountDto
            { AccountId = LastId, Name = "AccountTest" + LastId, Password = new Sha512Hasher().Hash("test") };
            acc = await AccountDao.TryInsertOrUpdateAsync(acc);
            var sessionRefHolder = new SessionRefHolder();
            var handlers = packetHandlers ?? new List<IPacketHandler>
            {
                new CharNewPacketHandler(CharacterDao, new Mock<IItemGenerationService>().Object, new Mock<IDao<QuicklistEntryDto, Guid>>().Object,
                        new Mock<IDao<IItemInstanceDto?, Guid>>().Object, new Mock<IDao<InventoryItemInstanceDto, Guid>>().Object, new HpService(), new MpService(), WorldConfiguration, new Mock<IDao<CharacterSkillDto, Guid>>().Object, ItemList, NullLoggerFactory.Instance),
                new BlInsPackettHandler(BlacklistHttpClient.Object, NullLogger<BlInsPackettHandler>.Instance, Instance.LogLanguageLocalizer),
                new UseItemPacketHandler(new Mock<Wolverine.IMessageBus>().Object),
                new FinsPacketHandler(FriendHttpClient.Object, ChannelHttpClient.Object, TestHelpers.Instance.PubSubHub.Object, Instance.SessionRegistry),
                new SelectPacketHandler(CharacterDao, NullLogger<SelectPacketHandler>.Instance, NullLoggerFactory.Instance, new Mock<IItemGenerationService>().Object, MapInstanceAccessorService,
                    ItemInstanceDao, InventoryItemInstanceDao, StaticBonusDao, new Mock<IDao<QuicklistEntryDto, Guid>>().Object, new Mock<IDao<TitleDto, Guid>>().Object, new Mock<IDao<CharacterQuestDto, Guid>>().Object,
                    new Mock<IDao<CharacterQuestObjectiveDto, Guid>>().Object,
                    new Mock<IDao<RespawnDto, long>>().Object, new Mock<IDao<ScriptDto, Guid>>().Object, new List<QuestDto>(), new List<QuestObjectiveDto>(),WorldConfiguration, Instance.LogLanguageLocalizer, Instance.PubSubHub.Object, Instance.Clock, ItemList, new HpService(), new MpService(), SessionGroupFactory, new CharacterInitializationService(), new Mock<Wolverine.IMessageBus>().Object),
                new CSkillPacketHandler(Instance.Clock),
                new CBuyPacketHandler(new Mock<IBazaarHub>().Object, new Mock<IItemGenerationService>().Object, NullLogger<CBuyPacketHandler>.Instance, ItemInstanceDao, Instance.LogLanguageLocalizer),
                new CRegPacketHandler(WorldConfiguration, new Mock<IBazaarHub>().Object, ItemInstanceDao, InventoryItemInstanceDao),
                new CScalcPacketHandler(WorldConfiguration, new Mock<IBazaarHub>().Object, new Mock<IItemGenerationService>().Object, NullLogger<CScalcPacketHandler>.Instance, ItemInstanceDao, Instance.LogLanguageLocalizer)
            };
            var packetHandlerRegistry = new NosCore.GameObject.Services.PacketHandlerService.PacketHandlerRegistry(handlers);
            var session = new ClientSession(
                NullLogger<ClientSession>.Instance,
                packetHandlerRegistry,
                new Mock<ILogLanguageLocalizer<NosCore.Networking.Resource.LogLanguageKey>>().Object,
                Instance.LogLanguageLocalizer,
                TestHelpers.Instance.PubSubHub.Object,
                new Mock<IEncoder>().Object,
                new WorldPacketHandlingStrategy(NullLogger<WorldPacketHandlingStrategy>.Instance, Instance.LogLanguageLocalizer, sessionRefHolder),
                new List<ISessionDisconnectHandler>(),
                Instance.SessionRegistry,
                new Mock<NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub.IAuthHub>().Object,
                Instance.GameLanguageLocalizer)
            {
                SessionId = LastId
            };

            var characterDto = new CharacterDto
            {
                CharacterId = LastId,
                Name = "TestExistingCharacter" + LastId,
                Slot = 1,
                AccountId = acc.AccountId,
                MapId = 1,
                State = CharacterState.Active,
                Level = 1,
                JobLevel = 1,
                Hp = 100,
                Mp = 100,
                Class = CharacterClassType.Adventurer,
                Gender = GenderType.Male,
                HairStyle = HairStyleType.HairStyleA,
                HairColor = HairColorType.Black
            };
            await CharacterDao.TryInsertOrUpdateAsync(characterDto);
            var mockChannel = new Mock<IChannel>();
            mockChannel.Setup(s => s.Id).Returns(Guid.NewGuid().ToString());
            session.RegisterChannel(mockChannel.Object);
            session.InitializeAccount(acc);

            var mapInstance = MapInstanceAccessorService.GetBaseMapById(0)!;
            var hpService = new HpService();
            var mpService = new MpService();
            var maxHp = (int)hpService.GetHp(characterDto.Class, characterDto.Level);
            var maxMp = (int)mpService.GetMp(characterDto.Class, characterDto.Level);

            var playerEntity = mapInstance.EcsWorld.CreatePlayer(
                (int)characterDto.CharacterId,
                characterDto.CharacterId,
                acc.AccountId,
                characterDto.Name ?? string.Empty,
                mapInstance.MapInstanceId,
                characterDto.MapX,
                characterDto.MapY,
                2,
                characterDto.Hp,
                maxHp,
                characterDto.Mp,
                maxMp,
                characterDto.Level,
                characterDto.LevelXp,
                characterDto.JobLevel,
                characterDto.JobLevelXp,
                characterDto.HeroLevel,
                characterDto.HeroXp,
                characterDto.Gold,
                characterDto.Reput,
                (short)characterDto.Dignity,
                (short)characterDto.Compliment,
                characterDto.Gender,
                characterDto.HairStyle,
                characterDto.HairColor,
                characterDto.Class,
                0,
                10,
                acc.Authority,
                acc.Authority >= AuthorityType.GameMaster,
                WorldConfiguration.Value.ServerId);

            var now = Instance.Clock.GetCurrentInstant();
            var group = new GameObject.Services.GroupService.Group(NosCore.Data.Enumerations.Group.GroupType.Group, SessionGroupFactory);
            var inventoryService = new InventoryService(ItemList, WorldConfiguration, NullLogger<InventoryService>.Instance);
            var playerStateComponent = new GameObject.Ecs.Components.PlayerStateComponent(
                characterDto,
                acc,
                null,
                false,
                false,
                false,
                false,
                true,
                now,
                now,
                0
            );

            mapInstance.EcsWorld.AddComponent(playerEntity, playerStateComponent);
            mapInstance.EcsWorld.AddComponent(playerEntity, new GameObject.Ecs.Components.PlayerNetworkComponent(session, session.Channel));
            mapInstance.EcsWorld.AddComponent(playerEntity, new GameObject.Ecs.Components.PlayerContextComponent(mapInstance, group, null));
            mapInstance.EcsWorld.AddComponent(playerEntity, new GameObject.Ecs.Components.PlayerInventoryComponent(
                inventoryService,
                new ConcurrentDictionary<short, NosCore.GameObject.Services.BattleService.CharacterSkill>(),
                new ConcurrentDictionary<Guid, NosCore.GameObject.Services.QuestService.CharacterQuest>(),
                new List<QuicklistEntryDto>(),
                new List<StaticBonusDto>(),
                new List<TitleDto>(),
                new List<RespawnDto>()));
            mapInstance.EcsWorld.AddComponent(playerEntity, new GameObject.Ecs.Components.PlayerSocialComponent(
                new ConcurrentDictionary<long, long>(),
                null));
            mapInstance.EcsWorld.AddComponent(playerEntity, new GameObject.Ecs.Components.PlayerRequestsComponent(
                new Dictionary<Type, System.Reactive.Subjects.Subject<RequestData>>
                {
                    { typeof(NpcDialogRequestSubject), new System.Reactive.Subjects.Subject<RequestData>() }
                }));
            session.SetPlayerEntity(playerEntity, mapInstance.EcsWorld);

            var character = session.Character;
            group.JoinGroup(character);

            session.Account = acc;
            return session;
        }

        public static async Task ResetAsync()
        {
            Lazy = new Lazy<TestHelpers>(() => new TestHelpers());
            Instance.InitDatabase();
            await Instance.GenerateMapInstanceProviderAsync();
        }
    }
}
