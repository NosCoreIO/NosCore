//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InstantCombatService;
using NosCore.Tests.Shared;
using SpecLight;
using InstantCombat = NosCore.GameObject.Services.InstantCombatService.InstantCombatService;

namespace NosCore.GameObject.Tests.Services.InstantCombatService
{
    [TestClass]
    public class InstantCombatServiceTests
    {
        private static readonly byte[] Brackets = [1, 40, 50, 60, 70, 80];

        private TestWorld World;
        private readonly List<ClientSession> _players = new();
        private List<InstantCombatRoom> _rooms = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
        }

        [TestMethod]
        public async Task PlayersFightWithTheirLevelBracket()
        {
            await new Spec("Each player fights in the highest bracket their level reaches")
                .GivenAsync(PlayersOfLevels_, new byte[] { 10, 39, 45, 85 })
                .When(GroupingThePlayers)
                .Then(TheRoomsShouldBe_, "1:10,39 40:45 80:85")
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ABracketSplitsPastFiftyPlayers()
        {
            await new Spec("A bracket with more than 50 players opens another room")
                .GivenAsync(PlayersOfLevels_, Enumerable.Repeat((byte)20, 51).ToArray())
                .When(GroupingThePlayers)
                .Then(TheRoomSizesShouldBe_, "50 1")
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARunningCombatIsNotStartedAgain()
        {
            await new Spec("A combat already running on the channel is not started a second time")
                .When(StartingACombat)
                .Then(StartingAnotherShouldBeRefused)
                .ExecuteAsync();
        }

        private async Task PlayersOfLevels_(byte[] levels)
        {
            foreach (var level in levels)
            {
                var session = await World.CreateSessionAsync();
                session.Character.Level = level;
                _players.Add(session);
            }
        }

        private void GroupingThePlayers() => _rooms = InstantCombat.GroupIntoRooms(_players, Brackets).ToList();

        private void TheRoomsShouldBe_(string expected) =>
            Assert.AreEqual(expected, string.Join(" ", _rooms.Select(r =>
                $"{r.LevelMinimum}:{string.Join(",", r.Players.Select(p => p.Character.Level))}")));

        private void TheRoomSizesShouldBe_(string expected) =>
            Assert.AreEqual(expected, string.Join(" ", _rooms.Select(r => r.Players.Count)));

        private IInstantCombatService _service;

        private void StartingACombat()
        {
            _service = World.Create<InstantCombat>();
            Assert.IsTrue(_service.TryStart(false));
        }

        private void StartingAnotherShouldBeRefused() => Assert.IsFalse(_service.TryStart(false));
    }
}
