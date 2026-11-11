//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.FamilyService;
using NosCore.PacketHandlers.Family;
using NosCore.Packets.ClientPackets.Families;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Families;
using NosCore.Tests.Shared;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DataFamilyLogType = NosCore.Data.Enumerations.Family.FamilyLogType;
using FamilyAuthority = NosCore.Data.Enumerations.Family.FamilyAuthority;

namespace NosCore.PacketHandlers.Tests.Family
{
    [TestClass]
    public class FamilyHistoryRankingAndTodayPacketHandlerTests
    {
        private TestWorld World;
        private const long FamilyId = 42;
        private const long OtherId = 900;
        private readonly List<FamilyLogDto> _logs = new();
        private readonly List<FamilyDto> _families = new();
        private readonly List<FamilyCharacterDto> _memberships = new();
        private readonly List<CharacterDto> _characters = new();
        private ClientSession _session;
        private GameObject.Services.FamilyService.Family _family;
        private FamilyLogService _familyLogService;
        private FamilyRankingService _familyRankingService;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _logs.Clear();
            _families.Clear();
            _memberships.Clear();
            _characters.Clear();
            _session = await World.CreateSessionAsync();
            _family = new GameObject.Services.FamilyService.Family
            {
                FamilyId = FamilyId,
                Name = "Testers",
                Members = new List<FamilyCharacterDto>
                {
                    new() { FamilyId = FamilyId, CharacterId = _session.Character.CharacterId, Authority = FamilyAuthority.Head }
                }
            };
            _session.Character.Family = _family;

            var pubSubHub = new Mock<IPubSubHub>();
            pubSubHub.Setup(s => s.SendMessageAsync(It.IsAny<IMessage>())).ReturnsAsync(true);
            _familyLogService = new FamilyLogService(ListDao(_logs), World.Clock, pubSubHub.Object);
            var familyDao = ListDao(_families);
            Mock.Get(familyDao).Setup(s => s.LoadAll()).Returns(() => _families);
            _familyRankingService = new FamilyRankingService(familyDao, ListDao(_memberships), ListDao(_characters));
            _session.LastPackets.Clear();
        }

        private static IDao<T, long> ListDao<T>(List<T> rows) where T : class
        {
            var dao = new Mock<IDao<T, long>>();
            dao.Setup(s => s.Where(It.IsAny<Expression<Func<T, bool>>>()))
                .Returns((Expression<Func<T, bool>> p) => rows.Where(p.Compile()));
            return dao.Object;
        }

        [TestMethod]
        public async Task TheHeadReadsTheHistory()
        {
            await new Spec("The history lists each entry's type, its arguments and how many hours ago it happened")
                .Given(TheFamilyLogged_With_And__HoursAgo, DataFamilyLogType.UserManaged, "Newbie", "Head", 3)
                .WhenAsync(AskingForTheHistory)
                .Then(TheHistoryShouldHoldOneEntry_With_And__HoursAgo, FamilyLogType.UserManaged, "Newbie", "Head", 3)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AMemberCannotReadTheHistoryWhileTheFamilyForbidsIt()
        {
            await new Spec("A plain member gets no history while members may not read it")
                .Given(TheCharacterIsA_, FamilyAuthority.Member)
                .And(MembersMayReadTheHistory_, false)
                .And(TheFamilyLogged_With_And__HoursAgo, DataFamilyLogType.UserManaged, "Newbie", "Head", 3)
                .WhenAsync(AskingForTheHistory)
                .Then(NoHistoryShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AMemberReadsTheHistoryOnceTheFamilyAllowsIt()
        {
            await new Spec("A plain member gets the history once members may read it")
                .Given(TheCharacterIsA_, FamilyAuthority.Member)
                .And(MembersMayReadTheHistory_, true)
                .And(TheFamilyLogged_With_And__HoursAgo, DataFamilyLogType.UserManaged, "Newbie", "Head", 3)
                .WhenAsync(AskingForTheHistory)
                .Then(TheHistoryShouldHoldOneEntry_With_And__HoursAgo, FamilyLogType.UserManaged, "Newbie", "Head", 3)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AKeeperFollowsTheKeeperPermission()
        {
            await new Spec("A keeper gets no history while keepers may not read it, even if members may")
                .Given(TheCharacterIsA_, FamilyAuthority.Manager)
                .And(MembersMayReadTheHistory_, true)
                .WhenAsync(AskingForTheHistory)
                .Then(NoHistoryShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NoFamilyMeansNoHistory()
        {
            await new Spec("A character without a family gets no history")
                .Given(TheCharacterHasNoFamily)
                .WhenAsync(AskingForTheHistory)
                .Then(NoHistoryShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheExperienceRankingOrdersByLevelThenExperience()
        {
            await new Spec("Families rank by level, then by experience, and a tie shares its rank")
                .Given(AFamily_AtLevel_With_Experience, "Alpha", (byte)3, 100)
                .And(AFamily_AtLevel_With_Experience, "Beta", (byte)5, 10)
                .And(AFamily_AtLevel_With_Experience, "Gamma", (byte)3, 100)
                .And(AFamily_AtLevel_With_Experience, "Delta", (byte)3, 50)
                .WhenAsync(AskingForTheRanking_, FamilyRankingType.Experience)
                .Then(TheRankingShouldBe_, new[] { "1|Beta|5|10", "2|Alpha|3|100", "2|Gamma|3|100", "4|Delta|3|50" })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheReputationRankingAddsUpEveryMember()
        {
            await new Spec("The reputation ranking adds up the reputation of every member")
                .Given(AFamily_AtLevel_With_Experience, "Alpha", (byte)1, 0)
                .And(AFamily_AtLevel_With_Experience, "Beta", (byte)2, 0)
                .And(_HasAMemberWith_Reputation, "Alpha", 300L)
                .And(_HasAMemberWith_Reputation, "Alpha", 400L)
                .And(_HasAMemberWith_Reputation, "Beta", 500L)
                .WhenAsync(AskingForTheRanking_, FamilyRankingType.CurrentDemons)
                .Then(TheRankingShouldBe_, new[] { "1|Alpha|1|700", "2|Beta|2|500" })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheRankingNamesTheTabItWasAskedFor()
        {
            await new Spec("The ranking reply names the tab it fills, so the client puts the first family in it")
                .Given(AFamily_AtLevel_With_Experience, "Alpha", (byte)3, 100)
                .WhenAsync(AskingForTheRanking_, FamilyRankingType.CurrentDemons)
                .Then(TheRankingTabShouldBe_, FamilyRankingType.CurrentDemons)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AMonthlyTabIsEmpty()
        {
            await new Spec("Nothing records monthly experience, so that tab comes back empty rather than wrong")
                .Given(AFamily_AtLevel_With_Experience, "Alpha", (byte)3, 100)
                .WhenAsync(AskingForTheRanking_, FamilyRankingType.MonthlyExperience)
                .Then(TheRankingShouldBe_, Array.Empty<string>())
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFamilyMemberOpensTheDailyMessageBox()
        {
            await new Spec("A family member asking to write today's message gets the box")
                .WhenAsync(AskingToWriteTodaysMessage)
                .Then(TheDailyMessageBoxShouldOpen_, true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NoFamilyMeansNoDailyMessageBox()
        {
            await new Spec("A character without a family gets no daily message box")
                .Given(TheCharacterHasNoFamily)
                .WhenAsync(AskingToWriteTodaysMessage)
                .Then(TheDailyMessageBoxShouldOpen_, false)
                .ExecuteAsync();
        }

        private void TheFamilyLogged_With_And__HoursAgo(DataFamilyLogType type, string first, string second, int hours) =>
            _logs.Add(new FamilyLogDto
            {
                FamilyId = FamilyId,
                FamilyLogType = type,
                FirstArgument = first,
                SecondArgument = second,
                Timestamp = World.Clock.GetCurrentInstant() - Duration.FromHours(hours)
            });

        private void TheCharacterIsA_(FamilyAuthority authority) => _family.Members = new List<FamilyCharacterDto>
        {
            new() { FamilyId = FamilyId, CharacterId = OtherId, Authority = FamilyAuthority.Head },
            new() { FamilyId = FamilyId, CharacterId = _session.Character.CharacterId, Authority = authority }
        };

        private void MembersMayReadTheHistory_(bool allowed) => _family.MemberCanGetHistory = allowed;

        private void TheCharacterHasNoFamily() => _session.Character.Family = null;

        private void AFamily_AtLevel_With_Experience(string name, byte level, int experience) => _families.Add(new FamilyDto
        {
            FamilyId = _families.Count + 1,
            Name = name,
            FamilyLevel = level,
            FamilyExperience = experience
        });

        private void _HasAMemberWith_Reputation(string familyName, long reputation)
        {
            var characterId = _characters.Count + 1000;
            _characters.Add(new CharacterDto { CharacterId = characterId, Name = $"c{characterId}", Reput = reputation });
            _memberships.Add(new FamilyCharacterDto
            {
                FamilyId = _families.Single(s => s.Name == familyName).FamilyId,
                CharacterId = characterId
            });
        }

        private Task AskingForTheHistory() =>
            new FhistCtsPacketHandler(_familyLogService, World.Clock).ExecuteAsync(new FhistCtsPacket(), _session);

        private Task AskingForTheRanking_(FamilyRankingType type) =>
            new FrankCtsPacketHandler(_familyRankingService).ExecuteAsync(new FrankCtsPacket { Type = type }, _session);

        private Task AskingToWriteTodaysMessage() => World.Create<TodayPacketHandler>().ExecuteAsync(new TodayPacket(), _session);

        private void TheHistoryShouldHoldOneEntry_With_And__HoursAgo(FamilyLogType type, string first, string second, int hours)
        {
            var entry = _session.LastPackets.OfType<GhisPacket>().Single().Entries.NotNull().Single().NotNull();
            Assert.AreEqual(type, entry.Type);
            Assert.AreEqual(first, entry.FirstArgument);
            Assert.AreEqual(second, entry.SecondArgument);
            Assert.IsNull(entry.ThirdArgument);
            Assert.AreEqual(hours, entry.HoursAgo);
        }

        private void NoHistoryShouldBeSent() => Assert.IsFalse(_session.LastPackets.OfType<GhisPacket>().Any());

        private void TheRankingTabShouldBe_(FamilyRankingType type) =>
            Assert.AreEqual(type, _session.LastPackets.OfType<FrankStcPacket>().Single().Type);

        private void TheRankingShouldBe_(string[] rows) => CollectionAssert.AreEqual(rows,
            _session.LastPackets.OfType<FrankStcPacket>().Single().Entries.NotNull()
                .Select(s => $"{s.NotNull().Rank}|{s.NotNull().FamilyName}|{s.NotNull().FamilyLevel}|{s.NotNull().Value}").ToList());

        private void TheDailyMessageBoxShouldOpen_(bool opened) =>
            Assert.AreEqual(opened, _session.LastPackets.OfType<TodayStcPacket>().Any());
    }
}
