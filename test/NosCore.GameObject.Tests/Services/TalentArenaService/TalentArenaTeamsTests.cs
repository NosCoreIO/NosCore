//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Services.TalentArenaService;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.TalentArenaService
{
    [TestClass]
    public class TalentArenaTeamsTests
    {
        private List<TalentArenaEntrant> _team = new();
        private List<TalentArenaEntrant> _other = new();

        [TestMethod]
        public async Task PlayersWithinFiveLevelsTeamUp()
        {
            await new Spec("Players whose levels lie within five of each other form a team")
                .Given(ATeamOfLevels_, "80,82")
                .And(APlayerOfLevel_, 85)
                .Then(TheyShouldTeamUp_, true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PlayersSixLevelsApartDoNotTeamUp()
        {
            await new Spec("A player six levels away from a teammate is left out")
                .Given(ATeamOfLevels_, "80,82")
                .And(APlayerOfLevel_, 86)
                .Then(TheyShouldTeamUp_, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFullTeamTakesNoFourthPlayer()
        {
            await new Spec("A team of three takes nobody else")
                .Given(ATeamOfLevels_, "80,80,80")
                .And(APlayerOfLevel_, 80)
                .Then(TheyShouldTeamUp_, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FullTeamsWithCloseAveragesAreRivals()
        {
            await new Spec("Two full teams whose average levels lie within five meet")
                .Given(ATeamOfLevels_, "70,72,74")
                .And(AnOpposingTeamOfLevels_, "75,77,79")
                .Then(TheyShouldBeRivals_, true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FullTeamsWithDistantAveragesAreNotRivals()
        {
            await new Spec("Two full teams whose average levels lie six apart do not meet")
                .Given(ATeamOfLevels_, "70,72,74")
                .And(AnOpposingTeamOfLevels_, "76,78,80")
                .Then(TheyShouldBeRivals_, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnIncompleteTeamHasNoRival()
        {
            await new Spec("A team still short of three players is not matched")
                .Given(ATeamOfLevels_, "80,80")
                .And(AnOpposingTeamOfLevels_, "80,80,80")
                .Then(TheyShouldBeRivals_, false)
                .ExecuteAsync();
        }

        private static List<TalentArenaEntrant> Entrants(string levels) =>
            levels.Split(',').Select((level, i) => new TalentArenaEntrant(i, byte.Parse(level))).ToList();

        private void ATeamOfLevels_(string levels) => _team = Entrants(levels);

        private void AnOpposingTeamOfLevels_(string levels) => _other = Entrants(levels);

        private void APlayerOfLevel_(int level) => _other = new List<TalentArenaEntrant> { new(99, (byte)level) };

        private void TheyShouldTeamUp_(bool expected) => Assert.AreEqual(expected, TalentArenaTeams.CanTeamUp(_team, _other));

        private void TheyShouldBeRivals_(bool expected) => Assert.AreEqual(expected, TalentArenaTeams.AreRivals(_team, _other));
    }
}
