//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.FactionWarService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Families;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.FactionWarService
{
    [TestClass]
    public class FactionWarServiceTests
    {
        private TestWorld World;
        private GameObject.Services.FactionWarService.FactionWarService _service;
        private ClientSession _angel;
        private ClientSession _demon;
        private ClientSession _elsewhere;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _angel = await World.CreateSessionAsync();
            _demon = await World.CreateSessionAsync();
            _elsewhere = await World.CreateSessionAsync();
            var glacernon = World.GetBaseMap(TestWorld.TestMapId);
            glacernon.IsAct4 = true;
            _angel.Character.MapInstance = glacernon;
            _demon.Character.MapInstance = glacernon;
            _elsewhere.Character.MapInstance = World.GetBaseMap(TestWorld.ShopMapId);
            _angel.Character.Faction = FactionType.Angel;
            _demon.Character.Faction = FactionType.Demon;
            _angel.Character.Level = 50;
            _demon.Character.Level = 55;
            _service = World.Create<GameObject.Services.FactionWarService.FactionWarService>();
        }

        [TestMethod]
        public async Task AnEnemyKillMovesTheKillersSideOnePercent()
        {
            await new Spec("Killing an enemy moves the killer's side one percent closer to its raid")
                .WhenAsync(TheAngelKillsTheDemon_Times, 3)
                .Then(TheAngelSideShouldBeAt_, 300)
                .And(TheDemonSideShouldBeAt_, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ProgressStopsAtFull()
        {
            await new Spec("A side never goes past one hundred percent")
                .WhenAsync(TheAngelKillsTheDemon_Times, 101)
                .Then(TheAngelSideShouldBeAt_, 10000)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AKillIsCountedForBothPlayers()
        {
            await new Spec("The killer scores a kill and two points, the victim a death and loses a point")
                .Given(TheDemonHas_Points, 5)
                .WhenAsync(TheAngelKillsTheDemon_Times, 1)
                .Then(TheAngelShouldHave_Kills_Deaths_Points, 1, 0, 2)
                .And(TheDemonShouldHave_Kills_Deaths_Points, 0, 1, 4)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFarWeakerVictimGivesNoPoints()
        {
            await new Spec("A victim more than ten levels away from the killer gives no points")
                .Given(TheDemonIsLevel_, (byte)30)
                .WhenAsync(TheAngelKillsTheDemon_Times, 1)
                .Then(TheAngelShouldHave_Kills_Deaths_Points, 1, 0, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PointsDoNotGoBelowZero()
        {
            await new Spec("A victim without points stays at zero")
                .WhenAsync(TheAngelKillsTheDemon_Times, 1)
                .Then(TheDemonShouldHave_Kills_Deaths_Points, 0, 1, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ASameFactionKillChangesNothing()
        {
            await new Spec("A kill between two players of one faction is not part of the war")
                .Given(TheDemonIsAnAngel)
                .WhenAsync(TheAngelKillsTheDemon_Times, 1)
                .Then(TheAngelSideShouldBeAt_, 0)
                .And(TheAngelShouldHave_Kills_Deaths_Points, 0, 0, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GlacernonHearsOfTheKill()
        {
            await new Spec("Everyone in Glacernon is told who defeated whom and gets the new progress")
                .WhenAsync(TheAngelKillsTheDemon_Times, 1)
                .Then(TheKillShouldBeAnnouncedTo_In_, "angel", SayColorType.Green)
                .And(TheKillShouldBeAnnouncedTo_In_, "demon", SayColorType.Red)
                .And(TheHudOf_ShouldShowTheAngelsAt_, "demon", (byte)1)
                .And(NobodyOutsideGlacernonShouldHearOfIt)
                .ExecuteAsync();
        }

        [TestMethod]
        public void TheHudCountsDownToTheMonthlyReset()
        {
            World.Clock.Reset(Instant.FromUtc(2026, 11, 30, 23, 0));
            var fc = _service.GenerateFc(_demon.Character);
            Assert.AreEqual(FactionType.Demon, fc.Faction);
            Assert.AreEqual(60, fc.MinutesUntilReset);
            Assert.AreEqual(Act4WarMode.Gathering, fc.Angel?.Mode);
            Assert.AreEqual(Act4WarMode.Gathering, fc.Demon?.Mode);
        }

        private async Task TheAngelKillsTheDemon_Times(int kills)
        {
            for (var i = 0; i < kills; i++)
            {
                await _service.RecordKillAsync(_angel.Character, _demon.Character);
            }
        }

        private void TheDemonHas_Points(int points) => _demon.Character.CharacterDto.Act4Points = points;

        private void TheDemonIsLevel_(byte level) => _demon.Character.Level = level;

        private void TheDemonIsAnAngel() => _demon.Character.Faction = FactionType.Angel;

        private void TheAngelSideShouldBeAt_(int progress) =>
            Assert.AreEqual(progress, _service.Progress(FactionType.Angel));

        private void TheDemonSideShouldBeAt_(int progress) =>
            Assert.AreEqual(progress, _service.Progress(FactionType.Demon));

        private void TheAngelShouldHave_Kills_Deaths_Points(int kills, int deaths, int points) =>
            AssertCounters(_angel, kills, deaths, points);

        private void TheDemonShouldHave_Kills_Deaths_Points(int kills, int deaths, int points) =>
            AssertCounters(_demon, kills, deaths, points);

        private static void AssertCounters(ClientSession session, int kills, int deaths, int points)
        {
            Assert.AreEqual(kills, session.Character.CharacterDto.Act4Kill);
            Assert.AreEqual(deaths, session.Character.CharacterDto.Act4Dead);
            Assert.AreEqual(points, session.Character.CharacterDto.Act4Points);
        }

        private ClientSession Named(string who) => who == "angel" ? _angel : _demon;

        private void TheKillShouldBeAnnouncedTo_In_(string who, SayColorType color)
        {
            var say = Named(who).LastPackets.OfType<Sayi2Packet>().Single();
            Assert.AreEqual(Game18NConstString.PlayerHasDefeteadPlayerX, say.Message);
            Assert.AreEqual(color, say.Type);
            CollectionAssert.AreEqual(new object[] { _angel.Character.Name, _demon.Character.Name }, say.Game18NArguments.ToArray());
        }

        private void TheHudOf_ShouldShowTheAngelsAt_(string who, byte percentage) =>
            Assert.AreEqual(percentage, Named(who).LastPackets.OfType<FcPacket>().Single().Angel?.Percentage);

        private void NobodyOutsideGlacernonShouldHearOfIt()
        {
            Assert.IsFalse(_elsewhere.LastPackets.OfType<Sayi2Packet>().Any());
            Assert.IsFalse(_elsewhere.LastPackets.OfType<FcPacket>().Any());
        }
    }
}
