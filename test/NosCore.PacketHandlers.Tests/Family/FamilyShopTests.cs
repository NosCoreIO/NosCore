//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.DignityService;
using NosCore.Algorithm.FamilyExperienceService;
using NosCore.Algorithm.FamilyUpgradeService;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Family;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.FamilyHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.ChannelCommunicationService.Handlers;
using NosCore.GameObject.Services.FamilyService;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.PacketHandlers.Shops;
using NosCore.Packets;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Families;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using NosCore.Tests.Shared.BDD.Steps;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FamilyAuthority = NosCore.Data.Enumerations.Family.FamilyAuthority;
using FamilyEntity = NosCore.Database.Entities.Family;

namespace NosCore.PacketHandlers.Tests.Family
{
    [TestClass]
    public class FamilyShopTests
    {
        private TestWorld World;
        private const int ManagerId = 100;
        private const short Warehouse = 9694;
        private const short WarehouseExpansion = 9695;
        private const short Members = 9696;
        private const short MembersExpansion = 9697;

        // Item.dat VNUM price and DATA level / line / tier of the family manager's four extensions.
        private static readonly List<FamilyUpgradeDto> Upgrades = new()
        {
            new() { ItemVNum = Warehouse, FamilyLevelMinimum = 2, UpgradeType = FamilyUpgradeType.Warehouse, UpgradeLevel = 0 },
            new() { ItemVNum = WarehouseExpansion, FamilyLevelMinimum = 7, UpgradeType = FamilyUpgradeType.Warehouse, UpgradeLevel = 1 },
            new() { ItemVNum = Members, FamilyLevelMinimum = 5, UpgradeType = FamilyUpgradeType.MemberLimit, UpgradeLevel = 0 },
            new() { ItemVNum = MembersExpansion, FamilyLevelMinimum = 9, UpgradeType = FamilyUpgradeType.MemberLimit, UpgradeLevel = 1 },
        };

        private static readonly List<ItemDto> Items = new()
        {
            new() { VNum = Warehouse, Type = NoscorePocketType.Main, Price = 500000 },
            new() { VNum = WarehouseExpansion, Type = NoscorePocketType.Main, Price = 2000000 },
            new() { VNum = Members, Type = NoscorePocketType.Main, Price = 5000000 },
            new() { VNum = MembersExpansion, Type = NoscorePocketType.Main, Price = 10000000 },
        };

        private ClientSession _head;
        private ClientSession _member;
        private IDao<FamilyDto, long> _familyDao;
        private IDao<FamilyCharacterDto, long> _familyCharacterDao;
        private FamilyService _familyService;
        private ShoppingPacketHandler _shopping;
        private BuyPacketHandler _buy;
        private FamilyChangedDataMessageChannelCommunicationMessageHandler _familyChanged;
        private readonly List<FamilyChangedData> _published = new();
        private long _familyId;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _head = await World.GenerateSessionAsync();
            _member = await World.GenerateSessionAsync();
            _published.Clear();

            var context = World.DbContextBuilder;
            _familyDao = new Dao<FamilyEntity, FamilyDto, long>(NullLogger<Dao<FamilyEntity, FamilyDto, long>>.Instance, context);
            _familyCharacterDao = new Dao<Database.Entities.FamilyCharacter, FamilyCharacterDto, long>(
                NullLogger<Dao<Database.Entities.FamilyCharacter, FamilyCharacterDto, long>>.Instance, context);
            var familyLogDao = new Dao<Database.Entities.FamilyLog, FamilyLogDto, long>(
                NullLogger<Dao<Database.Entities.FamilyLog, FamilyLogDto, long>>.Instance, context);
            var familyUpgradeDao = new Dao<Database.Entities.FamilyUpgrade, FamilyUpgradeDto, short>(
                NullLogger<Dao<Database.Entities.FamilyUpgrade, FamilyUpgradeDto, short>>.Instance, context);
            await familyUpgradeDao.TryInsertOrUpdateAsync(Upgrades);

            _familyId = (await _familyDao.TryInsertOrUpdateAsync(new FamilyDto
            {
                Name = "Testers",
                FamilyLevel = 1,
                MaxSize = 50,
                FamilyMessage = string.Empty
            })).FamilyId;
            await _familyCharacterDao.TryInsertOrUpdateAsync(new[]
            {
                Membership(_head, FamilyAuthority.Head),
                Membership(_member, FamilyAuthority.Member)
            });

            _familyService = new FamilyService(_familyDao, _familyCharacterDao, familyLogDao, World.CharacterDao);
            var master = new FamilyExtensionService(_familyDao, _familyCharacterDao, familyUpgradeDao, new FamilyUpgradeService());
            var familyHub = new Mock<IFamilyHub>();
            familyHub.Setup(s => s.PurchaseExtensionAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<short>()))
                .Returns((long familyId, long characterId, short vnum) => master.PurchaseAsync(familyId, characterId, vnum));
            var pubSubHub = new Mock<IPubSubHub>();
            pubSubHub.Setup(s => s.SendMessageAsync(It.IsAny<IMessage>()))
                .Callback((IMessage message) =>
                {
                    if (message is FamilyChangedData changed)
                    {
                        _published.Add(changed);
                    }
                })
                .ReturnsAsync(true);
            pubSubHub.Setup(s => s.GetSubscribersAsync()).ReturnsAsync(new List<Subscriber>());

            var shop = new FamilyShopService(Upgrades, Items, new FamilyUpgradeService(), familyHub.Object, pubSubHub.Object, _familyService);
            var dignity = new Mock<IDignityService>();
            dignity.Setup(s => s.GetLevelFromDignity(It.IsAny<short>())).Returns(DignityType.Default);
            _shopping = new ShoppingPacketHandler(NullLogger<ShoppingPacketHandler>.Instance, dignity.Object,
                World.LogLanguageLocalizer, World.SessionRegistry, World.NpcDialogService, shop);
            _buy = new BuyPacketHandler(NullLogger<BuyPacketHandler>.Instance, World.LogLanguageLocalizer,
                World.SessionRegistry, World.WorldConfiguration,
                World.GenerateItemProvider(), World.NpcDialogService,
                new Mock<NosCore.GameObject.Services.ShellOptionService.IShellOptionService>().Object,
                new Mock<NosCore.Dao.Interfaces.IDao<NosCore.Data.Dto.IItemInstanceDto?, Guid>>().Object, shop);

            var sessions = new[] { _head, _member };
            var registry = new Mock<ISessionRegistry>();
            registry.Setup(s => s.GetSessions(It.IsAny<Func<ClientSession, bool>?>()))
                .Returns((Func<ClientSession, bool>? predicate) => predicate == null ? sessions : sessions.Where(predicate));
            var management = new FamilyManagementService(_familyService, pubSubHub.Object, new FamilyExperienceService(),
                new Mock<IDaoTransactionScope> { DefaultValue = DefaultValue.Mock }.Object, registry.Object,
                new Mock<IFamilyWarehouseService>().Object, new Mock<IFamilyLogService>().Object);
            _familyChanged = new FamilyChangedDataMessageChannelCommunicationMessageHandler(registry.Object, _familyService,
                World.GameLanguageLocalizer, management);

            _head.Character.MapInstance.LoadNpcs(
                new List<MapNpcDto>
                {
                    new() { MapNpcId = ManagerId, Dialog = 462, MapId = _head.Character.MapInstance.Map.MapId, MapX = 1, MapY = 1, VNum = 1 }
                },
                new List<NpcMonsterDto> { new() { NpcMonsterVNum = 1 } });
            var manager = _head.Character.MapInstance.GetNpcById(ManagerId).NotNull();
            manager.Shop = new NosCore.GameObject.Services.ShopService.Shop { ShopType = FamilyShopService.ShopType };
            _head.WithWindowOpenedAt(ManagerId, NpcDialogWindow.Shop);
        }

        [TestMethod]
        public async Task TheManagerListsEveryExtensionAsAShopRow()
        {
            await new Spec("The list shows all four extensions with their price, whatever the family level")
                .GivenAsync(TheFamilyIsLevel_, (byte)2)
                .WhenAsync(TheHeadOpensTheFamilyShop)
                .Then(TheListShouldRead_, "n_inv 2 100 0 100 1.0.9694.1.500000 1.1.9695.1.2000000 1.2.9696.1.5000000 1.3.9697.1.10000000")
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheWarehouseIsSoldFromFamilyLevelTwo()
        {
            await new Spec("A level 2 family's head buys the 21-slot warehouse for 500 000 gold")
                .GivenAsync(TheFamilyIsLevel_, (byte)2)
                .And(TheHeadHasGold_, 600000L)
                .WhenAsync(TheHeadBuysSlot_, (short)0)
                .ThenAsync(TheWarehouseShouldHave_, (byte)21)
                .And(TheHeadShouldHaveGold_, 100000L)
                .And(TheHeadShouldBeToldItWasPurchased)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheWarehouseIsRefusedBelowLevelTwo()
        {
            await new Spec("A level 1 family cannot buy the warehouse")
                .GivenAsync(TheFamilyIsLevel_, (byte)1)
                .And(TheHeadHasGold_, 600000L)
                .WhenAsync(TheHeadBuysSlot_, (short)0)
                .Then(TheHeadShouldBeTold_, Game18NConstString.FamilyLevelInsufficient)
                .AndAsync(TheWarehouseShouldHave_, (byte)0)
                .And(TheHeadShouldHaveGold_, 600000L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheWarehouseExpansionIsSoldFromFamilyLevelSeven()
        {
            await new Spec("A level 7 family with the 21-slot warehouse expands it to 49 for 2 000 000 gold")
                .GivenAsync(TheFamilyIsLevel_, (byte)7)
                .AndAsync(TheFamilyAlreadyHasAWarehouseOf_, (byte)21)
                .And(TheHeadHasGold_, 2000000L)
                .WhenAsync(TheHeadBuysSlot_, (short)1)
                .ThenAsync(TheWarehouseShouldHave_, (byte)49)
                .And(TheHeadShouldHaveGold_, 0L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheWarehouseExpansionIsRefusedBelowLevelSeven()
        {
            await new Spec("A level 6 family cannot expand its warehouse")
                .GivenAsync(TheFamilyIsLevel_, (byte)6)
                .AndAsync(TheFamilyAlreadyHasAWarehouseOf_, (byte)21)
                .And(TheHeadHasGold_, 2000000L)
                .WhenAsync(TheHeadBuysSlot_, (short)1)
                .Then(TheHeadShouldBeTold_, Game18NConstString.FamilyLevelInsufficient)
                .AndAsync(TheWarehouseShouldHave_, (byte)21)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheWarehouseExpansionNeedsTheWarehouseFirst()
        {
            await new Spec("Expanding a warehouse the family never bought is refused")
                .GivenAsync(TheFamilyIsLevel_, (byte)7)
                .And(TheHeadHasGold_, 2000000L)
                .WhenAsync(TheHeadBuysSlot_, (short)1)
                .Then(TheHeadShouldBeTold_, Game18NConstString.RequestToPurchaseStandardVersionFirst)
                .AndAsync(TheWarehouseShouldHave_, (byte)0)
                .And(TheHeadShouldHaveGold_, 2000000L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SeventyMembersAreSoldFromFamilyLevelFive()
        {
            await new Spec("A level 5 family raises its member limit from 50 to 70 for 5 000 000 gold")
                .GivenAsync(TheFamilyIsLevel_, (byte)5)
                .And(TheHeadHasGold_, 5000000L)
                .WhenAsync(TheHeadBuysSlot_, (short)2)
                .ThenAsync(TheMemberLimitShouldBe_, (byte)70)
                .And(TheHeadShouldHaveGold_, 0L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SeventyMembersAreRefusedBelowLevelFive()
        {
            await new Spec("A level 4 family cannot raise its member limit")
                .GivenAsync(TheFamilyIsLevel_, (byte)4)
                .And(TheHeadHasGold_, 5000000L)
                .WhenAsync(TheHeadBuysSlot_, (short)2)
                .Then(TheHeadShouldBeTold_, Game18NConstString.FamilyLevelInsufficient)
                .AndAsync(TheMemberLimitShouldBe_, (byte)50)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AHundredMembersAreSoldFromFamilyLevelNine()
        {
            await new Spec("A level 9 family with 70 members raises the limit to 100 for 10 000 000 gold")
                .GivenAsync(TheFamilyIsLevel_, (byte)9)
                .AndAsync(TheFamilyAlreadyHasAMemberLimitOf_, (byte)70)
                .And(TheHeadHasGold_, 10000000L)
                .WhenAsync(TheHeadBuysSlot_, (short)3)
                .ThenAsync(TheMemberLimitShouldBe_, (byte)100)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AHundredMembersAreRefusedBelowLevelNine()
        {
            await new Spec("A level 8 family cannot raise its member limit to 100")
                .GivenAsync(TheFamilyIsLevel_, (byte)8)
                .AndAsync(TheFamilyAlreadyHasAMemberLimitOf_, (byte)70)
                .And(TheHeadHasGold_, 10000000L)
                .WhenAsync(TheHeadBuysSlot_, (short)3)
                .Then(TheHeadShouldBeTold_, Game18NConstString.FamilyLevelInsufficient)
                .AndAsync(TheMemberLimitShouldBe_, (byte)70)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnExtensionIsRefusedWithoutTheGold()
        {
            await new Spec("A head short of the price keeps the gold and the family is unchanged")
                .GivenAsync(TheFamilyIsLevel_, (byte)2)
                .And(TheHeadHasGold_, 499999L)
                .WhenAsync(TheHeadBuysSlot_, (short)0)
                .Then(TheHeadShouldBeTold_, Game18NConstString.InsufficientGoldAvailable)
                .AndAsync(TheWarehouseShouldHave_, (byte)0)
                .And(TheHeadShouldHaveGold_, 499999L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OnlyTheHeadCanBuyAnExtension()
        {
            await new Spec("A deputy cannot buy the warehouse")
                .GivenAsync(TheFamilyIsLevel_, (byte)2)
                .AndAsync(TheHeadIsDemotedToDeputy)
                .And(TheHeadHasGold_, 600000L)
                .WhenAsync(TheHeadBuysSlot_, (short)0)
                .Then(TheHeadShouldBeTold_, Game18NConstString.HeadCanPurchaseOnly)
                .AndAsync(TheWarehouseShouldHave_, (byte)0)
                .And(TheHeadShouldHaveGold_, 600000L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnExtensionAlreadyBoughtIsNotSoldTwice()
        {
            await new Spec("A family that has the warehouse is told so and keeps its gold")
                .GivenAsync(TheFamilyIsLevel_, (byte)2)
                .AndAsync(TheFamilyAlreadyHasAWarehouseOf_, (byte)21)
                .And(TheHeadHasGold_, 600000L)
                .WhenAsync(TheHeadBuysSlot_, (short)0)
                .Then(TheHeadShouldBeTold_, Game18NConstString.AlreadyPurchased)
                .And(TheHeadShouldHaveGold_, 600000L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MembersOnEveryChannelSeeTheNewMemberLimit()
        {
            await new Spec("After the purchase every member is refreshed and their ginfo shows 70 slots")
                .GivenAsync(TheFamilyIsLevel_, (byte)5)
                .And(TheHeadHasGold_, 5000000L)
                .WhenAsync(TheHeadBuysSlot_, (short)2)
                .AndAsync(ThePublishedFamilyChangeArrives)
                .Then(TheMemberShouldSeeAMemberLimitOf_, (byte)70)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TwoConcurrentPurchasesAreAppliedOnce()
        {
            await new Spec("Two buys of the warehouse racing each other charge once and set the size once")
                .GivenAsync(TheFamilyIsLevel_, (byte)2)
                .And(TheHeadHasGold_, 1000000L)
                .WhenAsync(TheHeadBuysSlotTwiceAtOnce_, (short)0)
                .ThenAsync(TheWarehouseShouldHave_, (byte)21)
                .And(TheHeadShouldHaveGold_, 500000L)
                .And(OnlyOnePurchaseShouldBeAnnounced)
                .ExecuteAsync();
        }

        private FamilyCharacterDto Membership(ClientSession session, FamilyAuthority authority) => new()
        {
            FamilyId = _familyId,
            CharacterId = session.Character.CharacterId,
            Authority = authority,
            DailyMessage = string.Empty
        };

        private async Task UpdateFamilyAsync(Action<FamilyDto> change)
        {
            var family = (await _familyDao.FirstOrDefaultAsync(s => s.FamilyId == _familyId)).NotNull();
            change(family);
            await _familyDao.TryInsertOrUpdateAsync(family);
            await RefreshAsync();
        }

        private async Task RefreshAsync()
        {
            _head.Character.Family = await _familyService.GetFamilyAsync(_head.Character.CharacterId);
            _member.Character.Family = await _familyService.GetFamilyAsync(_member.Character.CharacterId);
        }

        private Task TheFamilyIsLevel_(byte level) => UpdateFamilyAsync(s => s.FamilyLevel = level);

        private Task TheFamilyAlreadyHasAWarehouseOf_(byte size) => UpdateFamilyAsync(s => s.WarehouseSize = size);

        private Task TheFamilyAlreadyHasAMemberLimitOf_(byte limit) => UpdateFamilyAsync(s => s.MaxSize = limit);

        private async Task TheHeadIsDemotedToDeputy()
        {
            var headId = _head.Character.CharacterId;
            var membership = (await _familyCharacterDao.FirstOrDefaultAsync(s => s.CharacterId == headId)).NotNull();
            membership.Authority = FamilyAuthority.Assistant;
            await _familyCharacterDao.TryInsertOrUpdateAsync(membership);
            await RefreshAsync();
        }

        private void TheHeadHasGold_(long gold) => _head.Character.Gold = gold;

        private Task TheHeadOpensTheFamilyShop() => _shopping.ExecuteAsync(new ShoppingPacket
        {
            VisualType = VisualType.Npc,
            VisualId = ManagerId
        }, _head);

        private Task TheHeadBuysSlot_(short slot) => _buy.ExecuteAsync(Buy(slot), _head);

        private Task TheHeadBuysSlotTwiceAtOnce_(short slot) =>
            Task.WhenAll(Task.Run(() => _buy.ExecuteAsync(Buy(slot), _head)), Task.Run(() => _buy.ExecuteAsync(Buy(slot), _head)));

        private static BuyPacket Buy(short slot) => new()
        {
            VisualType = VisualType.Npc,
            VisualId = ManagerId,
            Slot = slot,
            Amount = 1
        };

        private async Task ThePublishedFamilyChangeArrives()
        {
            _member.LastPackets.Clear();
            foreach (var change in _published.ToList())
            {
                await _familyChanged.Handle(change);
            }
        }

        private void TheListShouldRead_(string expected)
        {
            var list = _head.LastPackets.OfType<NInvPacket>().Last();
            Assert.AreEqual(expected, Serializer().Serialize(new[] { (IPacket)list }).TrimEnd());
        }

        private async Task TheWarehouseShouldHave_(byte size) =>
            Assert.AreEqual(size, (await _familyDao.FirstOrDefaultAsync(s => s.FamilyId == _familyId)).NotNull().WarehouseSize);

        private async Task TheMemberLimitShouldBe_(byte limit) =>
            Assert.AreEqual(limit, (await _familyDao.FirstOrDefaultAsync(s => s.FamilyId == _familyId)).NotNull().MaxSize);

        private void TheHeadShouldHaveGold_(long gold) => Assert.AreEqual(gold, _head.Character.Gold);

        private void TheHeadShouldBeToldItWasPurchased()
        {
            var message = _head.LastPackets.OfType<Msgi2Packet>().Single(s => s.Message == Game18NConstString.PurchasedItemX);
            Assert.AreEqual(Warehouse.ToString(), message.Game18NArguments[0]);
        }

        private void TheHeadShouldBeTold_(Game18NConstString message) =>
            Assert.IsTrue(_head.LastPackets.OfType<MsgiPacket>().Any(s => s.Message == message),
                $"expected {message}, got {string.Join(", ", _head.LastPackets.Select(s => s?.GetType().Name))}");

        private void TheMemberShouldSeeAMemberLimitOf_(byte limit)
        {
            Assert.AreEqual(limit, _member.Character.Family?.MaxSize);
            Assert.AreEqual(limit, _member.LastPackets.OfType<GInfoPacket>().Last().MembersCapacity);
        }

        private void OnlyOnePurchaseShouldBeAnnounced()
        {
            Assert.AreEqual(1, _head.LastPackets.OfType<Msgi2Packet>().Count(s => s.Message == Game18NConstString.PurchasedItemX));
            Assert.AreEqual(1, _published.Count);
        }

        private static Serializer Serializer() => new(typeof(IPacket).Assembly.GetTypes()
            .Where(p => p.GetInterfaces().Contains(typeof(IPacket)) && p.IsClass && !p.IsAbstract)
            .ToList());
    }
}
