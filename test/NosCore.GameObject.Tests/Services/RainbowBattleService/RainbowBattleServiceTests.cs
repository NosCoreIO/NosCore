//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using SpecLight;
using RainbowBattle = NosCore.GameObject.Services.RainbowBattleService.RainbowBattleService;

namespace NosCore.GameObject.Tests.Services.RainbowBattleService
{
    [TestClass]
    public class RainbowBattleServiceTests
    {
        private TestWorld World;
        private RainbowBattle _service;
        private ClientSession _red;
        private ClientSession _blue;
        private MapInstance _map;
        private List<List<(long CharacterId, RainbowBattleTeamType Team)>> _battles = [];

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _service = World.Create<RainbowBattle>();
            _red = await World.CreateSessionAsync();
            _blue = await World.CreateSessionAsync();
            _map = World.GetBaseMap(0);
            foreach (var session in new[] { _red, _blue })
            {
                session.Character.MapInstance = _map;
            }
        }

        [TestMethod]
        public async Task OppositeTeamsFightOnTheBattleMap()
        {
            await new Spec("Players of opposite teams can hit each other on the battle map")
                .Given(TheMapIsARainbowBattle)
                .And(ThePlayersAreOn_And_, RainbowBattleTeamType.Red, RainbowBattleTeamType.Blue)
                .Then(TheyShouldBeOpponents_, true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TeammatesNeverFight()
        {
            await new Spec("Players of the same team never hit each other")
                .Given(TheMapIsARainbowBattle)
                .And(ThePlayersAreOn_And_, RainbowBattleTeamType.Red, RainbowBattleTeamType.Red)
                .Then(TheyShouldBeOpponents_, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFrozenPlayerCannotBeHit()
        {
            await new Spec("A frozen player is out of the fight until they thaw")
                .Given(TheMapIsARainbowBattle)
                .And(ThePlayersAreOn_And_, RainbowBattleTeamType.Red, RainbowBattleTeamType.Blue)
                .And(TheBluePlayerIsFrozen)
                .Then(TheyShouldBeOpponents_, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TeamsOnlyFightInsideTheBattle()
        {
            await new Spec("Rainbow Battle teams mean nothing outside the battle map")
                .Given(ThePlayersAreOn_And_, RainbowBattleTeamType.Red, RainbowBattleTeamType.Blue)
                .Then(TheyShouldBeOpponents_, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TooFewPlayersStartNothing()
        {
            await new Spec("Fewer registrants than a battle needs start no battle")
                .When(Splitting_Players, 9)
                .Then(ThereShouldBe_Battles, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TeamsAreDealtOutEvenly()
        {
            await new Spec("Ten registrants make one battle of five against five, the strongest on red")
                .When(Splitting_Players, 10)
                .Then(ThereShouldBe_Battles, 1)
                .And(BattleSizesShouldBe_, new[] { 10 })
                .And(EachBattleShouldHaveEvenTeams)
                .And(TheStrongestShouldBeRedAndTheNextTwoBlue)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFullBattleOpensASecondOne()
        {
            await new Spec("More registrants than one battle holds are shared between two battles of similar size")
                .When(Splitting_Players, 31)
                .Then(ThereShouldBe_Battles, 2)
                .And(BattleSizesShouldBe_, new[] { 16, 15 })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BiggerCrystalsScoreMore()
        {
            await new Spec("A large crystal is worth three points, a medium two and a small one")
                .Then(CrystalsShouldScore)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARunningBattleIsNotStartedAgain()
        {
            await new Spec("A Rainbow Battle already open on the channel is not started a second time")
                .When(StartingARainbowBattle)
                .Then(StartingAnotherShouldBeRefused)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NobodySignsUpWhileNothingIsOpen()
        {
            await new Spec("Asking to sign up while no Rainbow Battle is open does nothing")
                .WhenAsync(TheRedPlayerAsksToSignUp)
                .Then(TheRedPlayerShouldGetNoSearchWindow)
                .ExecuteAsync();
        }

        private void TheMapIsARainbowBattle() => _map.MapInstanceType = MapInstanceType.RainbowBattleInstance;

        private void ThePlayersAreOn_And_(RainbowBattleTeamType red, RainbowBattleTeamType blue)
        {
            _red.Character.RainbowBattleTeam = red;
            _blue.Character.RainbowBattleTeam = blue;
        }

        private void TheBluePlayerIsFrozen() =>
            _blue.Character.RainbowBattleThawsAt = World.Clock.GetCurrentInstant().Plus(Duration.FromSeconds(20));

        private void TheyShouldBeOpponents_(bool expected) =>
            Assert.AreEqual(expected, GameObject.Services.BattleService.BattleService.CanAttack(_red.Character, _blue.Character));

        private void Splitting_Players(int count) =>
            _battles = RainbowBattle.Split(
                Enumerable.Range(1, count).Select(i => ((long)i, (byte)(100 - i), (byte)0)).ToList(), 10, 30);

        private void ThereShouldBe_Battles(int count) => Assert.AreEqual(count, _battles.Count);

        private void BattleSizesShouldBe_(int[] sizes) =>
            CollectionAssert.AreEqual(sizes, _battles.Select(b => b.Count).ToArray());

        private void EachBattleShouldHaveEvenTeams()
        {
            foreach (var battle in _battles)
            {
                Assert.AreEqual(battle.Count(p => p.Team == RainbowBattleTeamType.Red),
                    battle.Count(p => p.Team == RainbowBattleTeamType.Blue));
            }
        }

        private void TheStrongestShouldBeRedAndTheNextTwoBlue() =>
            CollectionAssert.AreEqual(
                new[] { RainbowBattleTeamType.Red, RainbowBattleTeamType.Blue, RainbowBattleTeamType.Blue, RainbowBattleTeamType.Red },
                _battles[0].Take(4).Select(p => p.Team).ToArray());

        private void CrystalsShouldScore()
        {
            Assert.AreEqual(RainbowCrystalSize.Large, RainbowBattle.SizeOf(RainbowBattle.LargeCrystal));
            Assert.AreEqual(3, RainbowBattle.PointsOf(RainbowCrystalSize.Large));
            Assert.AreEqual(RainbowCrystalSize.Medium, RainbowBattle.SizeOf(RainbowBattle.MediumCrystal));
            Assert.AreEqual(2, RainbowBattle.PointsOf(RainbowCrystalSize.Medium));
            Assert.AreEqual(RainbowCrystalSize.Small, RainbowBattle.SizeOf(RainbowBattle.SmallCrystal));
            Assert.AreEqual(1, RainbowBattle.PointsOf(RainbowCrystalSize.Small));
        }

        private void StartingARainbowBattle() => Assert.IsTrue(_service.TryStart(true));

        private void StartingAnotherShouldBeRefused() => Assert.IsFalse(_service.TryStart(true));

        private Task TheRedPlayerAsksToSignUp() => _service.RegisterAsync(_red);

        private void TheRedPlayerShouldGetNoSearchWindow() => Assert.IsFalse(_red.LastPackets.OfType<BSInfoPacket>().Any());
    }
}
