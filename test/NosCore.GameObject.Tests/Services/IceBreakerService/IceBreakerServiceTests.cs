//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.IceBreakerService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Tests.Shared;
using SpecLight;
using IceBreaker = NosCore.GameObject.Services.IceBreakerService.IceBreakerService;

namespace NosCore.GameObject.Tests.Services.IceBreakerService
{
    [TestClass]
    public class IceBreakerServiceTests
    {
        private TestWorld World;
        private IIceBreakerService _service;
        private ClientSession _attacker;
        private ClientSession _defender;
        private ClientSession _third;
        private MapInstance _map;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _service = World.Create<IceBreaker>();
            _attacker = await World.CreateSessionAsync();
            _defender = await World.CreateSessionAsync();
            _third = await World.CreateSessionAsync();
            _map = World.GetBaseMap(0);
            foreach (var session in new[] { _attacker, _defender, _third })
            {
                session.Character.MapInstance = _map;
            }
        }

        [TestMethod]
        public async Task PlayersFightOnceTheMapTurnsToPvp()
        {
            await new Spec("Two players fight each other once the Icebreaker opens PvP")
                .Given(TheMapIsAnIcebreakerWithPvp)
                .Then(TheyShouldBeOpponents_, true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NobodyFightsBeforePvpOpens()
        {
            await new Spec("Players waiting in the Icebreaker cannot hit each other yet")
                .Then(TheyShouldBeOpponents_, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GroupMatesFightOnTheSameSide()
        {
            await new Spec("Members of the same group never hit each other")
                .Given(TheMapIsAnIcebreakerWithPvp)
                .And(TheAttackerAndDefenderAreGrouped)
                .Then(TheyShouldBeOpponents_, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFallenPlayerIsFrozen()
        {
            await new Spec("A player who falls is frozen on the spot with full HP, and cannot be hit any more")
                .Given(TheMapIsAnIcebreakerWithPvp)
                .WhenAsync(TheDefenderIsFrozen)
                .Then(TheDefenderShouldBeFrozenWithFullHp)
                .And(TheyShouldBeOpponents_, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheLastPlayerStandingWins()
        {
            await new Spec("The fight is decided when one player is left standing")
                .Then(TheFightBetween_ShouldBeDecided_, "attacker", true)
                .And(TheFightBetween_ShouldBeDecided_, "attacker defender", false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AGroupLeftStandingWinsTogether()
        {
            await new Spec("The fight is decided when everyone standing is in the same group")
                .Given(TheAttackerAndDefenderAreGrouped)
                .Then(TheFightBetween_ShouldBeDecided_, "attacker defender", true)
                .And(TheFightBetween_ShouldBeDecided_, "attacker defender third", false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NobodyJoinsOutsideTheSignUp()
        {
            await new Spec("Accepting the question once sign-up is over costs nothing")
                .Given(TheDefenderHas_Gold, 1000L)
                .WhenAsync(TheDefenderAcceptsTheQuestion)
                .Then(TheDefenderShouldHave_Gold, 1000L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARunningIcebreakerIsNotStartedAgain()
        {
            await new Spec("An Icebreaker already running on the channel is not started a second time")
                .When(StartingAnIcebreaker)
                .Then(StartingAnotherShouldBeRefused)
                .ExecuteAsync();
        }

        private void TheMapIsAnIcebreakerWithPvp()
        {
            _map.MapInstanceType = MapInstanceType.IceBreakerInstance;
            _map.IsPvp = true;
        }

        private void TheAttackerAndDefenderAreGrouped()
        {
            var group = new Group(GroupType.Group, World.SessionGroupFactory);
            _attacker.Character.JoinGroup(group);
            _defender.Character.JoinGroup(group);
        }

        private Task TheDefenderIsFrozen()
        {
            _defender.Character.Hp = 0;
            _defender.Character.IsAlive = false;
            return _service.FreezeAsync(_defender.Character);
        }

        private void TheDefenderShouldBeFrozenWithFullHp()
        {
            var defender = _defender.Character;
            Assert.IsTrue(defender.IsAlive);
            Assert.AreEqual(defender.MaxHp, defender.Hp);
            Assert.IsTrue(defender.NoMove);
            Assert.IsTrue(defender.NoAttack);
        }

        private void TheyShouldBeOpponents_(bool expected) =>
            Assert.AreEqual(expected, GameObject.Services.BattleService.BattleService.CanAttack(_attacker.Character, _defender.Character));

        private void TheFightBetween_ShouldBeDecided_(string names, bool expected)
        {
            var players = names.Split(' ').Select(n => n switch
            {
                "attacker" => _attacker.Character,
                "defender" => _defender.Character,
                _ => _third.Character,
            }).ToList();
            Assert.AreEqual(expected, IceBreaker.IsDecided(players));
        }

        private void TheDefenderHas_Gold(long gold) => _defender.Character.Gold = gold;

        private Task TheDefenderAcceptsTheQuestion() => _service.JoinAsync(_defender);

        private void TheDefenderShouldHave_Gold(long gold) => Assert.AreEqual(gold, _defender.Character.Gold);

        private void StartingAnIcebreaker() => Assert.IsTrue(_service.TryStart(true));

        private void StartingAnotherShouldBeRefused() => Assert.IsFalse(_service.TryStart(true));
    }
}
