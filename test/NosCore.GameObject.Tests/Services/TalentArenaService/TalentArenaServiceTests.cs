//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using TalentArena = NosCore.GameObject.Services.TalentArenaService.TalentArenaService;

namespace NosCore.GameObject.Tests.Services.TalentArenaService
{
    [TestClass]
    public class TalentArenaServiceTests
    {
        private TestWorld World;
        private ClientSession _attacker;
        private ClientSession _defender;
        private MapInstance _map;
        private TalentArena _service;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _service = World.Create<TalentArena>();
            _attacker = await World.CreateSessionAsync();
            _defender = await World.CreateSessionAsync();
            _map = World.GetBaseMap(0);
            _map.MapInstanceType = MapInstanceType.TalentArenaMapInstance;
            foreach (var session in new[] { _attacker, _defender })
            {
                session.Character.MapInstance = _map;
            }
        }

        [TestMethod]
        public async Task RingFightersOfOpposingTeamsFight()
        {
            await new Spec("The Zenas and Erenia fighters in the ring hit each other")
                .Given(TheAttackerFightsFor_, ArenaTeamType.Zenas)
                .And(TheDefenderFightsFor_, ArenaTeamType.Erenia)
                .And(BothStandInTheRing)
                .Then(TheyShouldBeOpponents_, true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FightersWaitForTheRoundToOpen()
        {
            await new Spec("Fighters cannot hit each other before their round opens")
                .Given(TheAttackerFightsFor_, ArenaTeamType.Zenas)
                .And(TheDefenderFightsFor_, ArenaTeamType.Erenia)
                .Then(TheyShouldBeOpponents_, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TeammatesNeverFight()
        {
            await new Spec("Two fighters of the same team never hit each other")
                .Given(TheAttackerFightsFor_, ArenaTeamType.Erenia)
                .And(TheDefenderFightsFor_, ArenaTeamType.Erenia)
                .And(BothStandInTheRing)
                .Then(TheyShouldBeOpponents_, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheMatchGoesOnWhileBothTeamsStand()
        {
            await new Spec("A match goes on while both teams have a fighter left")
                .Then(TheMatchWithDefeated_ShouldEndIn_, "Z1,E2", (ArenaResultType?)null)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheLastTeamStandingWins()
        {
            await new Spec("The team with a fighter left wins once the other team has fallen")
                .Then(TheMatchWithDefeated_ShouldEndIn_, "E1,E2,E3,Z1", (ArenaResultType?)ArenaResultType.ZenasWon)
                .And(TheMatchWithDefeated_ShouldEndIn_, "Z1,Z2,Z3", (ArenaResultType?)ArenaResultType.EreniaWon)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BothTeamsFallingIsADraw()
        {
            await new Spec("A last round that runs out leaves both teams down and ends in a draw")
                .Then(TheMatchWithDefeated_ShouldEndIn_, "Z1,Z2,Z3,E1,E2,E3", (ArenaResultType?)ArenaResultType.Draw)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheArenaOpensOnceAtATime()
        {
            await new Spec("The Arena of Talents cannot be opened twice")
                .Then(OpeningTheArenaShouldSucceed)
                .And(OpeningItAgainShouldBeRefused)
                .ExecuteAsync();
        }

        private void TheAttackerFightsFor_(ArenaTeamType team) => _attacker.Character.TalentArenaTeam = team;

        private void TheDefenderFightsFor_(ArenaTeamType team) => _defender.Character.TalentArenaTeam = team;

        private void BothStandInTheRing()
        {
            _attacker.Character.InTalentArenaRing = true;
            _defender.Character.InTalentArenaRing = true;
        }

        private void TheyShouldBeOpponents_(bool expected) =>
            Assert.AreEqual(expected, GameObject.Services.BattleService.BattleService.CanAttack(_attacker.Character, _defender.Character));

        private static void TheMatchWithDefeated_ShouldEndIn_(string defeated, ArenaResultType? expected)
        {
            var down = defeated.Split(',');
            var fighters = new[] { ArenaTeamType.Zenas, ArenaTeamType.Erenia }
                .SelectMany(team => Enumerable.Range(1, 3).Select(i => (Team: team, Name: $"{team.ToString()[0]}{i}")))
                .Select(f => (f.Team, down.Contains(f.Name)))
                .ToList();
            Assert.AreEqual(expected, TalentArena.Result(fighters));
        }

        private void OpeningTheArenaShouldSucceed() => Assert.IsTrue(_service.TryStart());

        private void OpeningItAgainShouldBeRefused() => Assert.IsFalse(_service.TryStart());
    }
}
