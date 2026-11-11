//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using AutoFixture;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NodaTime;
using NodaTime.Testing;
using NosCore.Algorithm.HpService;
using NosCore.Algorithm.MpService;
using NosCore.Core.Configuration;
using NosCore.Core.Encryption;
using NosCore.Core.I18N;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Packets.Enumerations;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.Database;
using NosCore.Database.Entities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceAccessService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MapItemGenerationService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.GameObject.Services.PacketHandlerService;
using NosCore.Networking;
using NosCore.Networking.SessionGroup;
using NosCore.PacketHandlers.Bazaar;
using NosCore.PacketHandlers.CharacterScreen;
using NosCore.PacketHandlers.Friend;
using NosCore.PacketHandlers.Inventory;
using NosCore.Packets.Interfaces;
using NosCore.PathFinder.Interfaces;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using NosCore.Tests.Shared.AutoFixture;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
    /// <summary>
    /// One game world per test: an in-memory database, three loaded maps, a clock and a session registry,
    /// plus a <see cref="NosCoreFixture"/> that builds any service or handler against them.
    /// </summary>
    /// <remarks>
    /// <c>Create&lt;T&gt;()</c> resolves the world's maps, DAOs, items and shared state as real objects and
    /// mocks everything else. <c>Freeze&lt;Mock&lt;IDep&gt;&gt;()</c> first to set one up.
    /// </remarks>
    public sealed class TestWorld
    {
        public const short TestMapId = 0;
        public const short ShopMapId = 1;
        public const short MinilandMapId = 20001;

        private static readonly ConditionalWeakTable<ClientSession, TestWorld> SessionWorlds = new();

        private int _lastId = 100;
        private readonly List<MapDto> _maps = new()
        {
            BlankMap(TestMapId, "testMap", false), BlankMap(ShopMapId, "shopMap", true), BlankMap(MinilandMapId, "miniland", true)
        };

        private TestWorld()
        {
            Fixture = new NosCoreFixture();
            Clock = Fixture.Create<FakeClock>();
            SessionRegistry = Fixture.Create<SessionRegistry>();
            WorldConfiguration = Fixture.Create<IOptions<WorldConfiguration>>();
            LogLanguageLocalizer = Fixture.Create<ILogLanguageLocalizer<LogLanguageKey>>();
            GameLanguageLocalizer = Fixture.Create<IGameLanguageLocalizer>();
            SessionGroupFactory = Fixture.Create<ISessionGroupFactory>();
            DistanceCalculator = Fixture.Create<IHeuristic>();

            PubSubHub = Fixture.Freeze<Mock<IPubSubHub>>();
            ChannelHub = Fixture.Freeze<Mock<IChannelHub>>();
            FriendHub = Fixture.Freeze<Mock<IFriendHub>>();
            FriendHub.Setup(s => s.GetFriendsAsync(It.IsAny<long>())).ReturnsAsync(new List<CharacterRelationStatus>());
            BlacklistHub = Fixture.Freeze<Mock<IBlacklistHub>>();
            BlacklistHub.Setup(s => s.GetBlacklistedAsync(It.IsAny<long>())).ReturnsAsync(new List<CharacterRelationStatus>());

            var options = new DbContextOptionsBuilder<NosCoreContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            DbContextBuilder = () => new NosCoreContext(options);
            Fixture.Inject(DbContextBuilder);
            Fixture.Inject<IDaoTransactionScope>(new DaoTransactionScope(DbContextBuilder));

            AccountDao = Dao<Account, AccountDto, long>();
            CharacterDao = Dao<Character, CharacterDto, long>();
            CharacterRelationDao = Dao<CharacterRelation, CharacterRelationDto, Guid>();
            MateDao = Dao<Mate, MateDto, long>();
            MinilandDao = Dao<Miniland, MinilandDto, Guid>();
            MinilandObjectDao = Dao<MinilandObject, MinilandObjectDto, Guid>();
            ItemInstanceDao = Dao<ItemInstance, IItemInstanceDto?, Guid>();
            InventoryItemInstanceDao = Dao<InventoryItemInstance, InventoryItemInstanceDto, Guid>();
            StaticBonusDao = Dao<StaticBonus, StaticBonusDto, long>();
            MapMonsterDao = Dao<MapMonster, MapMonsterDto, int>();
            MapNpcDao = Dao<MapNpc, MapNpcDto, int>();
            PortalDao = Dao<Portal, PortalDto, int>();
            ShopItemDao = Dao<ShopItem, ShopItemDto, int>();

            Fixture.Inject(ItemList);
            Fixture.UseReal<IItemGenerationService, ItemGenerationService>();
            Fixture.UseReal<INpcDialogService, NpcDialogService>();
            Fixture.Freeze<Mock<IMinilandService>>()
                .Setup(s => s.GetMinilandPortals(It.IsAny<long>())).Returns(new List<GameObject.Map.Portal>());

            MapItemProvider = Fixture.Create<MapItemGenerationService>();
            Fixture.Inject<IMapItemGenerationService>(MapItemProvider);
            MapInstanceRegistry = Fixture.Create<MapInstanceRegistry>();
            Fixture.Inject<IMapInstanceRegistry>(MapInstanceRegistry);
            MapInstanceAccessorService = Fixture.Create<MapInstanceAccessorService>();
            Fixture.Inject<IMapInstanceAccessorService>(MapInstanceAccessorService);
            MapChangeService = Fixture.Create<MapChangeService>();
            Fixture.Inject<IMapChangeService>(MapChangeService);
            Fixture.Inject(_maps);
            MapInstanceGeneratorService = Fixture.Create<MapInstanceGeneratorService>();
            Fixture.Inject<IMapInstanceGeneratorService>(MapInstanceGeneratorService);
        }

        public static async Task<TestWorld> CreateAsync()
        {
            GameObject.Ecs.ReputationLevels.Load(ClientLadders.ReputationLevels());
            GameObject.Ecs.DignityLevels.Load(ClientLadders.DignityLevels());
            var world = new TestWorld();
            await world.MapNpcDao.TryInsertOrUpdateAsync(new MapNpcDto());
            await world.MapInstanceGeneratorService.InitializeAsync();
            await world.MapInstanceGeneratorService.AddMapInstanceAsync(new MapInstance((Map)world._maps[2], world.MinilandId, false,
                MapInstanceType.NormalInstance, world.MapItemProvider, NullLogger<MapInstance>.Instance, world.Clock,
                world.MapChangeService, world.SessionGroupFactory, world.SessionRegistry, world.DistanceCalculator));
            return world;
        }

        public NosCoreFixture Fixture { get; }

        public T Create<T>() => Fixture.Create<T>();

        /// <summary>
        /// Freezes <typeparamref name="T"/>. A frozen <c>Mock&lt;IDep&gt;</c> also replaces whatever the world
        /// had registered for <c>IDep</c>, such as its clock or session registry.
        /// </summary>
        public T Freeze<T>()
        {
            var frozen = Fixture.Freeze<T>();
            if (frozen is Mock mock)
            {
                InjectMethod.MakeGenericMethod(mock.GetType().GetGenericArguments()[0])
                    .Invoke(null, new object[] { Fixture, mock.Object });
            }

            return frozen;
        }

        private static readonly System.Reflection.MethodInfo InjectMethod =
            typeof(FixtureRegistrar).GetMethod(nameof(FixtureRegistrar.Inject))
            ?? throw new InvalidOperationException("FixtureRegistrar.Inject is missing.");

        public void Inject<T>(T instance) where T : class => Fixture.Inject(instance);

        public TestWorld UseReal<TService, TImplementation>() where TImplementation : TService
        {
            Fixture.UseReal<TService, TImplementation>();
            return this;
        }

        public FakeClock Clock { get; }
        public SessionRegistry SessionRegistry { get; }
        public IOptions<WorldConfiguration> WorldConfiguration { get; }
        public ILogLanguageLocalizer<LogLanguageKey> LogLanguageLocalizer { get; }
        public IGameLanguageLocalizer GameLanguageLocalizer { get; }
        public ISessionGroupFactory SessionGroupFactory { get; }
        public IHeuristic DistanceCalculator { get; }

        public Mock<IPubSubHub> PubSubHub { get; }
        public Mock<IChannelHub> ChannelHub { get; }
        public Mock<IFriendHub> FriendHub { get; }
        public Mock<IBlacklistHub> BlacklistHub { get; }

        public Func<DbContext> DbContextBuilder { get; }
        public IDao<AccountDto, long> AccountDao { get; }
        public IDao<CharacterDto, long> CharacterDao { get; }
        public IDao<CharacterRelationDto, Guid> CharacterRelationDao { get; }
        public IDao<MateDto, long> MateDao { get; }
        public IDao<MinilandDto, Guid> MinilandDao { get; }
        public IDao<MinilandObjectDto, Guid> MinilandObjectDao { get; }
        public IDao<IItemInstanceDto?, Guid> ItemInstanceDao { get; }
        public IDao<InventoryItemInstanceDto, Guid> InventoryItemInstanceDao { get; }
        public IDao<StaticBonusDto, long> StaticBonusDao { get; }
        public IDao<MapMonsterDto, int> MapMonsterDao { get; }
        public IDao<MapNpcDto, int> MapNpcDao { get; }
        public IDao<PortalDto, int> PortalDao { get; }
        public IDao<ShopItemDto, int> ShopItemDao { get; }

        public List<ItemDto> ItemList { get; } = new()
        {
            new Item { Type = NoscorePocketType.Main, VNum = 1012, IsDroppable = true },
            new Item { Type = NoscorePocketType.Main, VNum = 1013 },
            new Item { Type = NoscorePocketType.Main, VNum = 1014, ItemType = ItemType.Production, Effect = ItemEffectType.OpenProduction },
            new Item { Type = NoscorePocketType.Etc, VNum = 1015 },
            new Item { Type = NoscorePocketType.Equipment, VNum = 1, ItemType = ItemType.Weapon },
            new Item { Type = NoscorePocketType.Equipment, VNum = 2, EquipmentSlot = EquipmentType.Fairy, Element = ElementType.Water },
            new Item { Type = NoscorePocketType.Equipment, VNum = 912, ItemType = ItemType.Specialist, ReputationMinimum = 2, Element = ElementType.Fire },
            new Item { Type = NoscorePocketType.Equipment, VNum = 924, ItemType = ItemType.Fashion },
            new Item
            {
                Type = NoscorePocketType.Main, VNum = 1078, ItemType = ItemType.Special,
                Effect = ItemEffectType.DroppedSpRecharger, EffectValue = 10_000, WaitDelay = 5_000
            }
        };

        public Guid MinilandId { get; } = Guid.NewGuid();
        public MapItemGenerationService MapItemProvider { get; }
        public MapInstanceRegistry MapInstanceRegistry { get; }
        public MapInstanceAccessorService MapInstanceAccessorService { get; }
        public MapChangeService MapChangeService { get; }
        public MapInstanceGeneratorService MapInstanceGeneratorService { get; }

        public MapInstance GetBaseMap(short mapId) => MapInstanceAccessorService.GetBaseMapById(mapId)
            ?? throw new InvalidOperationException($"Map {mapId} is not loaded.");

        public MapInstance GetMap(Guid mapInstanceId) => MapInstanceAccessorService.GetMapInstance(mapInstanceId)
            ?? throw new InvalidOperationException($"Map instance {mapInstanceId} is not loaded.");

        public IItemGenerationService GenerateItemProvider() => Create<ItemGenerationService>();

        public NpcDialogService NpcDialogService => Create<NpcDialogService>();

        public GroupLootService GroupLootService => Create<GroupLootService>();

        public async Task<ClientSession> CreateSessionAsync(List<IPacketHandler>? packetHandlers = null,
            List<ISessionDisconnectHandler>? disconnectHandlers = null)
        {
            var id = ++_lastId;
            var account = await AccountDao.TryInsertOrUpdateAsync(new AccountDto
            {
                AccountId = id,
                Name = "AccountTest" + id,
                Password = new Sha512Hasher().Hash("test")
            });

            var registry = new PacketHandlerRegistry(packetHandlers ?? new List<IPacketHandler>
            {
                Create<CharNewPacketHandler>(),
                Create<BlInsPackettHandler>(),
                Create<UseItemPacketHandler>(),
                Create<FinsPacketHandler>(),
                Create<SelectPacketHandler>(),
                Create<CSkillPacketHandler>(),
                Create<CBuyPacketHandler>(),
                Create<CRegPacketHandler>(),
                Create<CScalcPacketHandler>()
            });
            var session = new ClientSession(
                NullLogger<ClientSession>.Instance,
                registry,
                new Mock<ILogLanguageLocalizer<Networking.Resource.LogLanguageKey>>().Object,
                LogLanguageLocalizer,
                PubSubHub.Object,
                new Mock<NosCore.Networking.Encoding.IEncoder>().Object,
                new WorldPacketHandlingStrategy(NullLogger<WorldPacketHandlingStrategy>.Instance, LogLanguageLocalizer, new NosCore.Networking.SessionRef.SessionRefHolder()),
                disconnectHandlers ?? new List<ISessionDisconnectHandler>(),
                SessionRegistry,
                new Mock<GameObject.InterChannelCommunication.Hubs.AuthHub.IAuthHub>().Object)
            {
                SessionId = id
            };

            var characterDto = new CharacterDto
            {
                CharacterId = id,
                Name = "TestExistingCharacter" + id,
                Slot = 1,
                AccountId = account.AccountId,
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
            var channel = new Mock<IChannel>();
            channel.Setup(s => s.Id).Returns(Guid.NewGuid().ToString());
            session.RegisterChannel(channel.Object);
            session.InitializeAccount(account);

            var mapInstance = GetBaseMap(TestMapId);
            var playerEntity = mapInstance.EcsWorld.CreatePlayer(
                (int)characterDto.CharacterId,
                characterDto.CharacterId,
                account.AccountId,
                characterDto.Name ?? string.Empty,
                mapInstance.MapInstanceId,
                characterDto.MapX,
                characterDto.MapY,
                2,
                characterDto.Hp,
                (int)new HpService().GetHp(characterDto.Class, characterDto.Level),
                characterDto.Mp,
                (int)new MpService().GetMp(characterDto.Class, characterDto.Level),
                characterDto.Level,
                characterDto.LevelXp,
                characterDto.JobLevel,
                characterDto.JobLevelXp,
                characterDto.HeroLevel,
                characterDto.HeroXp,
                characterDto.Gold,
                characterDto.Reput,
                characterDto.SpPoint,
                characterDto.SpAdditionPoint,
                (short)characterDto.Dignity,
                (short)characterDto.Compliment,
                characterDto.Gender,
                characterDto.HairStyle,
                characterDto.HairColor,
                characterDto.Class,
                0,
                10,
                account.Authority,
                account.Authority >= AuthorityType.GameMaster,
                WorldConfiguration.Value.ServerId);

            var now = Clock.GetCurrentInstant();
            var group = new Group(Data.Enumerations.Group.GroupType.Group, SessionGroupFactory);
            var ecs = mapInstance.EcsWorld;
            ecs.AddComponent(playerEntity, new GameObject.Ecs.Components.PlayerStateComponent(
                characterDto, account, null, false, false, false, false, true, now, now, 0, now));
            ecs.AddComponent(playerEntity, new GameObject.Ecs.Components.PlayerNetworkComponent(session, session.Channel));
            ecs.AddComponent(playerEntity, new GameObject.Ecs.Components.PlayerContextComponent(mapInstance, group, null));
            ecs.AddComponent(playerEntity, new GameObject.Ecs.Components.PlayerInventoryComponent(
                new InventoryService(ItemList, WorldConfiguration, NullLogger<InventoryService>.Instance),
                new ConcurrentDictionary<short, GameObject.Services.BattleService.CharacterSkill>(),
                new ConcurrentDictionary<Guid, GameObject.Services.QuestService.CharacterQuest>(),
                new List<QuicklistEntryDto>(),
                new List<StaticBonusDto>(),
                new List<TitleDto>(),
                new List<RespawnDto>()));
            ecs.AddComponent(playerEntity, new GameObject.Ecs.Components.PlayerSocialComponent(
                new ConcurrentDictionary<long, long>(),
                null,
                null,
                new ConcurrentDictionary<long, long>(),
                new ConcurrentDictionary<long, long>(),
                new ConcurrentDictionary<long, Instant>(),
                null,
                new ConcurrentDictionary<(InviteType Type, long TargetId), Instant>()));
            ecs.AddComponent(playerEntity, new GameObject.Ecs.Components.PlayerMatesComponent(
                new ConcurrentDictionary<long, GameObject.Services.MateService.Mate>()));
            ecs.AddComponent(playerEntity, new GameObject.Ecs.Components.PlayerRequestsComponent(
                new Dictionary<Type, System.Reactive.Subjects.Subject<RequestData>>
                {
                    { typeof(NpcDialogRequestSubject), new System.Reactive.Subjects.Subject<RequestData>() }
                }));
            session.SetPlayerEntity(playerEntity, ecs);

            SessionWorlds.AddOrUpdate(session, this);
            group.JoinGroup(session.Character);
            session.Account = account;
            return session;
        }

        internal static TestWorld Of(ClientSession session) => SessionWorlds.TryGetValue(session, out var world)
            ? world
            : throw new InvalidOperationException("The session was not created by a TestWorld.");

        private IDao<TDto, TPk> Dao<TEntity, TDto, TPk>() where TEntity : class where TPk : struct
        {
            var dao = new Dao<TEntity, TDto, TPk>(NullLogger<Dao<TEntity, TDto, TPk>>.Instance, DbContextBuilder);
            Fixture.Inject<IDao<TDto, TPk>>(dao);
            return dao;
        }

        private static MapDto BlankMap(short mapId, string name, bool shopAllowed) => new Map
        {
            MapId = mapId,
            NameI18NKey = name,
            ShopAllowed = shopAllowed,
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
        };
    }

    public static class TestWorldSessionExtensions
    {
        public static TestWorld World(this ClientSession session) => TestWorld.Of(session);
    }
}
