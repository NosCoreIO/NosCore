//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NosCore.Core.Encryption;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Character;
using NosCore.GameObject.Services.CharacterRankingService;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using RankingService = NosCore.GameObject.Services.CharacterRankingService.CharacterRankingService;

namespace NosCore.GameObject.Tests.Services.CharacterRankingService
{
    [TestClass]
    public class CharacterRankingServiceTests
    {
        private TestWorld World;
        private RankingService _service;
        private long _nextId = 2000;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _service = World.Create<RankingService>();
        }

        [TestMethod]
        public async Task EachListRunsFromTheHighestValueDown()
        {
            await AddAsync("Ann", compliment: 5, reputation: 100, points: 30);
            await AddAsync("Bob", compliment: 9, reputation: 50, points: 10);
            await AddAsync("Cid", compliment: 1, reputation: 900, points: 20);

            var rankings = _service.GetRankings();

            CollectionAssert.AreEqual(new[] { "Bob", "Ann", "Cid" }, rankings.Popularity.Select(c => c.Name).ToArray());
            CollectionAssert.AreEqual(new[] { "Cid", "Ann", "Bob" }, rankings.Reputation.Select(c => c.Name).ToArray());
            CollectionAssert.AreEqual(new[] { "Ann", "Cid", "Bob" }, rankings.CombatPoints.Select(c => c.Name).ToArray());
            Assert.AreEqual(9, rankings.Popularity[0].Score);
            Assert.AreEqual(900, rankings.Reputation[0].Score);
            Assert.AreEqual(30, rankings.CombatPoints[0].Score);
        }

        [TestMethod]
        public async Task AnEntryCarriesTheCharactersLevels()
        {
            var id = await AddAsync("Ann", reputation: 100, level: 88, heroLevel: 12);

            var entry = _service.GetRankings().Reputation.Single();

            Assert.AreEqual(id, entry.CharacterId);
            Assert.AreEqual(88, entry.Level);
            Assert.AreEqual(12, entry.HeroLevel);
        }

        [TestMethod]
        public async Task ACharacterWithNothingToShowIsNotRanked()
        {
            await AddAsync("Ann", compliment: 0, reputation: 100, points: 0);

            var rankings = _service.GetRankings();

            Assert.AreEqual(0, rankings.Popularity.Count);
            Assert.AreEqual(1, rankings.Reputation.Count);
            Assert.AreEqual(0, rankings.CombatPoints.Count);
        }

        [TestMethod]
        public async Task StaffDeletedCharactersAndOtherServersAreLeftOut()
        {
            await AddAsync("Player", reputation: 10);
            await AddAsync("Staff", reputation: 1000, authority: AuthorityType.GameMaster);
            await AddAsync("Banned", reputation: 1000, authority: AuthorityType.Banned);
            await AddAsync("Deleted", reputation: 1000, state: CharacterState.Inactive);
            await AddAsync("Elsewhere", reputation: 1000, otherServer: true);

            CollectionAssert.AreEqual(new[] { "Player" }, _service.GetRankings().Reputation.Select(c => c.Name).ToArray());
        }

        [TestMethod]
        public async Task EachListStopsAtItsLength()
        {
            for (var i = 1; i <= 50; i++)
            {
                await AddAsync("C" + i, compliment: (short)i, reputation: i, points: i);
            }

            var rankings = _service.GetRankings();

            Assert.AreEqual(RankingService.PopularityPlaces, rankings.Popularity.Count);
            Assert.AreEqual(RankingService.ReputationPlaces, rankings.Reputation.Count);
            Assert.AreEqual(RankingService.CombatPointsPlaces, rankings.CombatPoints.Count);
            Assert.AreEqual("C50", rankings.Popularity[0].Name);
        }

        [TestMethod]
        public async Task TheListsChangeOnceADay()
        {
            await AddAsync("Ann", reputation: 100);
            Assert.AreEqual(1, _service.GetRankings().Reputation.Count);

            await AddAsync("Bob", reputation: 200);
            World.Clock.Advance(Duration.FromHours(1));
            Assert.AreEqual(1, _service.GetRankings().Reputation.Count, "an hour later the list is the one built that day");

            World.Clock.Advance(Duration.FromHours(24));
            CollectionAssert.AreEqual(new[] { "Bob", "Ann" }, _service.GetRankings().Reputation.Select(c => c.Name).ToArray());
        }

        [TestMethod]
        public async Task ThePacketsCarryTheLists()
        {
            await AddAsync("Ann", compliment: 5, reputation: 100, points: 30, level: 40, heroLevel: 3);

            var rankings = _service.GetRankings();
            var popularity = rankings.GeneratePopularityList().SubPackets.NotNull().Single().NotNull();
            var reputation = rankings.GenerateReputationList().SubPackets.NotNull().Single().NotNull();
            var points = rankings.GenerateCombatPointsList().SubPackets.NotNull().Single().NotNull();

            Assert.AreEqual(5, popularity.Compliment);
            Assert.AreEqual("Ann", popularity.CharacterName);
            Assert.AreEqual((byte)40, popularity.Level);
            Assert.AreEqual((byte)3, popularity.HeroLevel);
            Assert.AreEqual(100L, reputation.Reputation);
            Assert.AreEqual(30, points.Act4Points);
        }

        private async Task<long> AddAsync(string name, short compliment = 0, long reputation = 0, int points = 0,
            byte level = 1, byte heroLevel = 0, AuthorityType authority = AuthorityType.User,
            CharacterState state = CharacterState.Active, bool otherServer = false)
        {
            var id = _nextId++;
            await World.AccountDao.TryInsertOrUpdateAsync(new AccountDto
            {
                AccountId = id,
                Name = "RankedAccount" + id,
                Password = new Sha512Hasher().Hash("test"),
                Authority = authority
            });
            await World.CharacterDao.TryInsertOrUpdateAsync(new CharacterDto
            {
                CharacterId = id,
                AccountId = id,
                Name = name,
                Slot = 1,
                MapId = 1,
                State = state,
                Level = level,
                HeroLevel = heroLevel,
                Compliment = compliment,
                Reput = reputation,
                Act4Points = points,
                ServerId = (byte)(World.WorldConfiguration.Value.ServerId + (otherServer ? 1 : 0))
            });
            return id;
        }
    }
}
