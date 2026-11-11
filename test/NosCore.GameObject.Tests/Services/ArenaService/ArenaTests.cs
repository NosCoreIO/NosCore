//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ArenaService;
using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.ArenaService
{
    [TestClass]
    public class ArenaTests
    {
        private ClientSession _attacker;
        private ClientSession _defender;
        private MapInstance _map;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _attacker = await TestHelpers.Instance.GenerateSessionAsync();
            _defender = await TestHelpers.Instance.GenerateSessionAsync();
            _map = TestHelpers.Instance.GetBaseMap(0);
            _map.Map = ArenaTestMap.Create();
            Place(_attacker, 0, 0);
            Place(_defender, 1, 0);
        }

        [TestMethod]
        public async Task PlayersCannotFightOutsideAnArena()
        {
            await new Spec("Two players on an ordinary map are allies")
                .Then(TheyShouldBeOpponents_, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PlayersFightEachOtherInAnArena()
        {
            await new Spec("Two players on the arena floor can hit each other")
                .Given(TheMapIsAnArena)
                .Then(TheyShouldBeOpponents_, true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GroupMatesNeverFight()
        {
            await new Spec("Members of the same group stay allies in the arena")
                .Given(TheMapIsAnArena)
                .And(BothAreInTheSameGroup)
                .Then(TheyShouldBeOpponents_, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NobodyFightsOnThePlatform()
        {
            await new Spec("A player standing on the merchants' platform can neither hit nor be hit")
                .Given(TheMapIsAnArena)
                .And(TheDefenderStandsOnThePlatform)
                .Then(TheyShouldBeOpponents_, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NobodyFightsThemselves()
        {
            await new Spec("A player is never their own opponent")
                .Given(TheMapIsAnArena)
                .Then(TheAttackerShouldNotBeTheirOwnOpponent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheArenaOfGloryCostsTwiceAsMuch()
        {
            await new Spec("Entry costs 500 gold for the individual arena and 1 000 for the Arena of Glory")
                .Then(TheEntryCostOfType_ShouldBe_, (short)0, 500L)
                .And(TheEntryCostOfType_ShouldBe_, (short)1, 1000L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FightersArriveOnTheArenaFloor()
        {
            await new Spec("A fighter is dropped on a cell outside the platform")
                .Then(EveryRandomCellShouldBeOnTheFloor)
                .ExecuteAsync();
        }

        private void Place(ClientSession session, short x, short y)
        {
            session.Character.MapInstance = _map;
            session.Character.PositionX = x;
            session.Character.PositionY = y;
        }

        private void TheMapIsAnArena() => _map.MapInstanceType = MapInstanceType.ArenaInstance;

        private void BothAreInTheSameGroup()
        {
            var group = new Group(GroupType.Group, TestHelpers.Instance.SessionGroupFactory);
            _attacker.Character.JoinGroup(group);
            _defender.Character.JoinGroup(group);
        }

        private void TheDefenderStandsOnThePlatform() => Place(_defender, 2, 1);

        private void TheyShouldBeOpponents_(bool expected) =>
            Assert.AreEqual(expected, Arena.AreOpponents(_attacker.Character, _defender.Character));

        private void TheAttackerShouldNotBeTheirOwnOpponent() =>
            Assert.IsFalse(Arena.AreOpponents(_attacker.Character, _attacker.Character));

        private void TheEntryCostOfType_ShouldBe_(short type, long cost) =>
            Assert.AreEqual(cost, Arena.EntryCost(type));

        private void EveryRandomCellShouldBeOnTheFloor()
        {
            for (var i = 0; i < 50; i++)
            {
                var cell = Arena.RandomFightingCell(_map.Map);
                Assert.IsNotNull(cell);
                Assert.AreNotEqual((2, 1), ((int)cell.Value.X, (int)cell.Value.Y));
                Assert.AreNotEqual((3, 1), ((int)cell.Value.X, (int)cell.Value.Y));
            }
        }
    }

    internal static class ArenaTestMap
    {
        // A 4x2 arena: open floor, with a merchants' platform at (2,1) and a wall at (3,1).
        public static GameObject.Map.Map Create() => new()
        {
            MapId = Arena.IndividualArenaMapId,
            NameI18NKey = "arena",
            Data = new byte[]
            {
                4, 0, 2, 0,
                0, 0, 0, 0,
                0, 0, 18, 1,
            },
        };
    }
}
