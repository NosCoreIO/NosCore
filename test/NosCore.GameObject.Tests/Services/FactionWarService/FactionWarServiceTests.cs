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
using Moq;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.FactionWarService;
using NosCore.GameObject.Services.FamilyService;
using NosCore.GameObject.Services.MapInstanceAccessService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.ServerPackets.UI;
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
        private MapInstance _camp;
        private Mock<IFamilyContributionService> _familyContributions;

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
            _camp = glacernon;
            var maps = World.Freeze<Mock<IMapInstanceAccessorService>>();
            maps.Setup(m => m.GetBaseMapById(It.IsAny<short>())).Returns(glacernon);
            maps.Setup(m => m.GetMapInstance(glacernon.MapInstanceId)).Returns(glacernon);
            World.Inject(new List<NpcMonsterDto>
            {
                new() { NpcMonsterVNum = GameObject.Services.FactionWarService.FactionWarService.Mukraju, MaxHp = 1000, Level = 70 },
            });
            _familyContributions = World.Freeze<Mock<IFamilyContributionService>>();
            _service = World.Create<GameObject.Services.FactionWarService.FactionWarService>();
        }

        [TestMethod]
        public async Task AFullSideCallsLordMukraju()
        {
            await new Spec("At one hundred percent the side's bar empties and Lord Mukraju appears in its camp")
                .WhenAsync(TheAngelKillsTheDemon_Times, 100)
                .Then(TheAngelSideShouldBeIn_, Act4WarMode.Guardian)
                .And(TheAngelSideShouldBeAt_, 0)
                .And(LordMukrajuShouldStandInTheCamp_, true)
                .And(GlacernonShouldBeToldMukrajuAppearedInTheAngelCamp)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task KillsDoNotCountWhileMukrajuIsOut()
        {
            await new Spec("A side waiting on Lord Mukraju gains nothing from more kills")
                .GivenAsync(TheAngelKillsTheDemon_Times, 100)
                .WhenAsync(TheAngelKillsTheDemon_Times, 3)
                .Then(TheAngelSideShouldBeAt_, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task KillingMukrajuOpensARaid()
        {
            await new Spec("Killing Lord Mukraju opens one of the four raids to his side for an hour")
                .GivenAsync(TheAngelKillsTheDemon_Times, 100)
                .WhenAsync(LordMukrajuIsKilled)
                .Then(TheAngelSideShouldBeIn_, Act4WarMode.RaidOpen)
                .And(TheHudShouldShowTheOpenRaidFor_Seconds, 3600)
                .And(TheDemonSideShouldBeIn_, Act4WarMode.Gathering)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MukrajuLeavesWhenNotKilledInTime()
        {
            await new Spec("Lord Mukraju leaves after five minutes and the side starts gathering again")
                .GivenAsync(TheAngelKillsTheDemon_Times, 100)
                .WhenAsync(_MinutesPass, 5)
                .Then(TheAngelSideShouldBeIn_, Act4WarMode.Gathering)
                .And(LordMukrajuShouldStandInTheCamp_, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheRaidClosesAfterAnHour()
        {
            await new Spec("An open raid closes after an hour and the side starts gathering again")
                .GivenAsync(TheAngelKillsTheDemon_Times, 100)
                .AndAsync(LordMukrajuIsKilled)
                .WhenAsync(_MinutesPass, 60)
                .Then(TheAngelSideShouldBeIn_, Act4WarMode.Gathering)
                .And(NoRaidShouldBeOpenForTheAngels)
                .ExecuteAsync();
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
        public async Task ProgressCountsUpToTheRaid()
        {
            await new Spec("A side one kill short of its raid stands at ninety-nine percent")
                .WhenAsync(TheAngelKillsTheDemon_Times, 99)
                .Then(TheAngelSideShouldBeAt_, 9900)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MonsterKillsMoveTheKillersSide()
        {
            await new Spec("Each monster killed moves the killer's side a twentieth of a percent")
                .WhenAsync(TheAngelKills_Monsters, 20)
                .Then(TheAngelSideShouldBeAt_, 100)
                .And(TheDemonSideShouldBeAt_, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheHudOnlyHearsOfWholePercents()
        {
            await new Spec("Glacernon is only sent the progress once a monster kill moves the shown percentage")
                .WhenAsync(TheAngelKills_Monsters, 19)
                .Then(TheDemonShouldHaveBeenSent_Huds, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheHudShowsEachWholePercentFromMonsters()
        {
            await new Spec("The monster kill that completes a percent sends Glacernon the new progress")
                .WhenAsync(TheAngelKills_Monsters, 20)
                .Then(TheHudOf_ShouldShowTheAngelsAt_, "demon", (byte)1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MonsterKillsCanCallLordMukraju()
        {
            await new Spec("Monster kills alone can fill the bar and call Lord Mukraju")
                .WhenAsync(TheAngelKills_Monsters, 2000)
                .Then(TheAngelSideShouldBeIn_, Act4WarMode.Guardian)
                .And(LordMukrajuShouldStandInTheCamp_, true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ANeutralMonsterKillChangesNothing()
        {
            await new Spec("A player without a side moves no bar")
                .Given(TheAngelIsNeutral)
                .WhenAsync(TheAngelKills_Monsters, 20)
                .Then(TheAngelSideShouldBeAt_, 0)
                .And(TheDemonSideShouldBeAt_, 0)
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
        public async Task AKillCountsTowardsTheKillersFamilyMissions()
        {
            await new Spec("An enemy defeated by a family member counts towards that family's Glacernon missions")
                .Given(TheAngelIsInFamily_, 4L)
                .WhenAsync(TheAngelKillsTheDemon_Times, 2)
                .Then(Family_ShouldHaveBeenToldOf_DefeatedEnemies, 4L, 2)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AKillOutsideTheWarCountsForNoFamily()
        {
            await new Spec("A kill between two players of one faction counts towards no family mission")
                .Given(TheAngelIsInFamily_, 4L)
                .And(TheDemonIsAnAngel)
                .WhenAsync(TheAngelKillsTheDemon_Times, 1)
                .Then(Family_ShouldHaveBeenToldOf_DefeatedEnemies, 4L, 0)
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

        private void TheAngelIsInFamily_(long familyId) => _angel.Character.Family = new Family { FamilyId = familyId };

        private void Family_ShouldHaveBeenToldOf_DefeatedEnemies(long familyId, int kills)
        {
            var killer = _angel.Character.CharacterId;
            _familyContributions.Verify(s => s.GlacernonEnemyDefeatedAsync(familyId, killer), Times.Exactly(kills));
        }

        private async Task TheAngelKillsTheDemon_Times(int kills)
        {
            for (var i = 0; i < kills; i++)
            {
                await _service.RecordKillAsync(_angel.Character, _demon.Character);
            }
        }

        private async Task TheAngelKills_Monsters(int kills)
        {
            for (var i = 0; i < kills; i++)
            {
                await _service.RecordMonsterKillAsync(_angel.Character);
            }
        }

        private void TheAngelIsNeutral() => _angel.Character.Faction = FactionType.Neutral;

        private void TheDemonShouldHaveBeenSent_Huds(int count) =>
            Assert.AreEqual(count, _demon.LastPackets.OfType<FcPacket>().Count());

        private MonsterComponentBundle? Mukraju() => _camp.FindMonster(m =>
            m.VNum == GameObject.Services.FactionWarService.FactionWarService.Mukraju && m.IsAlive);

        private async Task LordMukrajuIsKilled()
        {
            Assert.IsTrue(Mukraju() is { } mukraju && await _service.GuardianDiedAsync(mukraju));
        }

        private async Task _MinutesPass(int minutes)
        {
            World.Clock.AdvanceMinutes(minutes);
            await _service.TickAsync();
        }

        private void TheAngelSideShouldBeIn_(Act4WarMode mode) => Assert.AreEqual(mode, _service.Mode(FactionType.Angel));

        private void TheDemonSideShouldBeIn_(Act4WarMode mode) => Assert.AreEqual(mode, _service.Mode(FactionType.Demon));

        private void LordMukrajuShouldStandInTheCamp_(bool standing) => Assert.AreEqual(standing, Mukraju() != null);

        private void GlacernonShouldBeToldMukrajuAppearedInTheAngelCamp() =>
            Assert.IsTrue(_demon.LastPackets.OfType<MsgiPacket>().Any(m => m.Message == Game18NConstString.LordMukrajuHasAppearedInThe));

        private void TheHudShouldShowTheOpenRaidFor_Seconds(int seconds)
        {
            var raid = _service.OpenRaid(FactionType.Angel);
            Assert.IsNotNull(raid);
            var angel = _service.GenerateFc(_angel.Character).Angel;
            Assert.AreEqual((short)seconds, angel?.SecondsLeft);
            Assert.AreEqual((short)seconds, angel?.TotalSeconds);
            Assert.AreEqual(raid == Act4RaidType.Morcos, angel?.IsMorcos);
            Assert.AreEqual(raid == Act4RaidType.Hatus, angel?.IsHatus);
            Assert.AreEqual(raid == Act4RaidType.Calvina, angel?.IsCalvina);
            Assert.AreEqual(raid == Act4RaidType.Berios, angel?.IsBerios);
        }

        private void NoRaidShouldBeOpenForTheAngels() => Assert.IsNull(_service.OpenRaid(FactionType.Angel));

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
