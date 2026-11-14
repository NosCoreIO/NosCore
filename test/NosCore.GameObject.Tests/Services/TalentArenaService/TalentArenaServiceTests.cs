//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.TalentArenaService;
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
        private readonly List<string> _fallen = new();
        private readonly List<string> _called = new();
        private int _callsUsed;

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
        public async Task AFinishedMatchIsCountedInTheFightersRecord()
        {
            await new Spec("The winning side gains a win, the losing side a loss, and a draw changes nothing")
                .Then(Zenas_RecordAfter_Is_, ArenaResultType.ZenasWon, "1/0")
                .And(Zenas_RecordAfter_Is_, ArenaResultType.EreniaWon, "0/1")
                .And(Zenas_RecordAfter_Is_, ArenaResultType.Draw, "0/0")
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

        [TestMethod]
        public async Task AFighterInTheRingCallsTheChosenTeammate()
        {
            await new Spec("Each call button brings in the matching teammate, in fighting order")
                .Then(A_CallingButton_ShouldBring_, "Z1", (byte)0, "Z2")
                .And(A_CallingButton_ShouldBring_, "Z1", (byte)1, "Z3")
                .And(A_CallingButton_ShouldBring_, "E1", (byte)1, "E3")
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OnlyTheFighterInTheRingCanCall()
        {
            await new Spec("A fighter waiting on the bench cannot call anyone")
                .Then(A_CallingButton_ShouldBring_, "Z2", (byte)0, (string?)null)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FallenOrAlreadyCalledTeammatesCannotBeCalled()
        {
            await new Spec("A teammate who has fallen or is already coming cannot be called")
                .Given(_HasFallen, "Z2")
                .And(_IsAlreadyCalled, "Z3")
                .Then(A_CallingButton_ShouldBring_, "Z1", (byte)0, (string?)null)
                .And(A_CallingButton_ShouldBring_, "Z1", (byte)1, (string?)null)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ATeamHasFiveCallsAMatch()
        {
            await new Spec("A team that has used its five calls cannot call again")
                .Given(TheTeamHasUsed_Calls, 4)
                .Then(A_CallingButton_ShouldBring_, "Z1", (byte)0, "Z2")
                .When(TheTeamHasUsed_Calls, 5)
                .Then(A_CallingButton_ShouldBring_, "Z1", (byte)0, (string?)null)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CallingOutsideAMatchDoesNothing()
        {
            await new Spec("A call sent outside the arena ring is ignored")
                .WhenAsync(TheAttackerCallsATeammateAsync)
                .Then(NothingShouldBeSentToTheAttacker)
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

        private static void Zenas_RecordAfter_Is_(ArenaResultType result, string record)
        {
            var dto = new CharacterDto();
            TalentArena.Record(dto, ArenaTeamType.Zenas, result);
            Assert.AreEqual(record, $"{dto.TalentWin}/{dto.TalentLose}");
        }

        private static void TheMatchWithDefeated_ShouldEndIn_(string defeated, ArenaResultType? expected)
        {
            var down = defeated.Split(',');
            var fighters = new[] { ArenaTeamType.Zenas, ArenaTeamType.Erenia }
                .SelectMany(team => Enumerable.Range(1, 3).Select(i => (Team: team, Name: $"{team.ToString()[0]}{i}")))
                .Select(f => (f.Team, down.Contains(f.Name)))
                .ToList();
            Assert.AreEqual(expected, TalentArena.Result(fighters));
        }

        private void _HasFallen(string name) => _fallen.Add(name);

        private void _IsAlreadyCalled(string name) => _called.Add(name);

        private void TheTeamHasUsed_Calls(int calls) => _callsUsed = calls;

        private void A_CallingButton_ShouldBring_(string caller, byte button, string? expected)
        {
            var names = new[] { ArenaTeamType.Zenas, ArenaTeamType.Erenia }
                .SelectMany(team => Enumerable.Range(1, 3).Select(i => (Team: team, Order: (byte)(i - 1), Name: $"{team.ToString()[0]}{i}")))
                .ToList();
            var fighters = names.Select((f, id) => new TalentArenaFighter(id, f.Team, f.Order, f.Order == 0,
                _fallen.Contains(f.Name), _called.Contains(f.Name))).ToList();
            long? expectedId = expected == null ? null : names.FindIndex(f => f.Name == expected);
            Assert.AreEqual(expectedId, TalentArena.CallTarget(fighters, names.FindIndex(f => f.Name == caller), button, _callsUsed));
        }

        private Task TheAttackerCallsATeammateAsync() => _service.CallAsync(_attacker, 0);

        private void NothingShouldBeSentToTheAttacker() => Assert.IsFalse(_attacker.LastPackets.Any());

        private void OpeningTheArenaShouldSucceed() => Assert.IsTrue(_service.TryStart());

        private void OpeningItAgainShouldBeRefused() => Assert.IsFalse(_service.TryStart());
    }
}
