//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class Act4BattleAndRevivalTests
    {
        private TestWorld World;
        private ClientSession _angel;
        private ClientSession _demon;
        private bool _canFight;
        private long? _revivalMapType;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _angel = await World.CreateSessionAsync();
            _demon = await World.CreateSessionAsync();
            _angel.Character.Faction = FactionType.Angel;
            _demon.Character.Faction = FactionType.Demon;
        }

        [TestMethod]
        public async Task AngelsAndDemonsFightInGlacernon()
        {
            await new Spec("An angel and a demon in Glacernon are opponents")
                .Given(BothStandInGlacernon)
                .When(TheAngelSizesUpTheDemon)
                .Then(TheyShouldBeAbleToFight_, true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AngelsAndDemonsDoNotFightElsewhere()
        {
            await new Spec("An angel and a demon on a normal map are not opponents")
                .When(TheAngelSizesUpTheDemon)
                .Then(TheyShouldBeAbleToFight_, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TwoAngelsDoNotFightInGlacernon()
        {
            await new Spec("Two angels in Glacernon are on the same side")
                .Given(BothStandInGlacernon)
                .And(TheDemonTurnsInto_, FactionType.Angel)
                .When(TheAngelSizesUpTheDemon)
                .Then(TheyShouldBeAbleToFight_, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ANeutralCharacterDoesNotFightInGlacernon()
        {
            await new Spec("A character with no faction takes no side in Glacernon")
                .Given(BothStandInGlacernon)
                .And(TheDemonTurnsInto_, FactionType.Neutral)
                .When(TheAngelSizesUpTheDemon)
                .Then(TheyShouldBeAbleToFight_, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFallenAngelRevivesAtTheAngelCitadel()
        {
            await new Spec("An angel who falls in Glacernon revives at the angel citadel")
                .Given(BothStandInGlacernon)
                .When(TheRevivalPointOfIsResolved_, FactionType.Angel)
                .Then(TheRevivalShouldUse_, (long?)RespawnType.Act4Angel)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFallenDemonRevivesAtTheDemonCitadel()
        {
            await new Spec("A demon who falls in Glacernon revives at the demon citadel")
                .Given(BothStandInGlacernon)
                .When(TheRevivalPointOfIsResolved_, FactionType.Demon)
                .Then(TheRevivalShouldUse_, (long?)RespawnType.Act4Demon)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFallOutsideGlacernonKeepsTheMapsRevivalPoint()
        {
            var expected = World.Create<RespawnService>().ResolveRespawnMapTypeId(_angel.Character.MapId);
            await new Spec("Outside Glacernon the faction does not change where a player revives")
                .When(TheRevivalPointOfIsResolved_, FactionType.Angel)
                .Then(TheRevivalShouldUse_, expected)
                .ExecuteAsync();
        }

        private void BothStandInGlacernon()
        {
            var glacernon = World.GetBaseMap(TestWorld.TestMapId);
            glacernon.IsAct4 = true;
            _angel.Character.MapInstance = glacernon;
            _demon.Character.MapInstance = glacernon;
        }

        private void TheDemonTurnsInto_(FactionType faction)
        {
            _demon.Character.Faction = faction;
        }

        private void TheAngelSizesUpTheDemon()
        {
            _canFight = GameObject.Services.BattleService.BattleService.AreAct4Opponents(_angel.Character, _demon.Character);
        }

        private void TheRevivalPointOfIsResolved_(FactionType faction)
        {
            var player = faction == FactionType.Angel ? _angel : _demon;
            _revivalMapType = World.Create<RespawnService>().ResolveRevivalMapTypeId(player.Character);
        }

        private void TheyShouldBeAbleToFight_(bool expected)
        {
            Assert.AreEqual(expected, _canFight);
        }

        private void TheRevivalShouldUse_(long? expected)
        {
            Assert.AreEqual(expected, _revivalMapType);
        }
    }
}
