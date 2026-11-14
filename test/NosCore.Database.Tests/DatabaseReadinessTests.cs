//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NosCore.Database.Tests
{
    [TestClass]
    public class DatabaseReadinessTests
    {
        [TestMethod]
        public void ADatabaseWithEveryMigrationHasNothingToSay()
        {
            Assert.IsNull(DatabaseReadiness.Describe([]));
        }

        [TestMethod]
        public void OneMissingMigrationIsNamed()
        {
            Assert.AreEqual(
                "The database is 1 migration behind this build (next: 20261113061457_AddSpeedBoostDash). " +
                "Run the parser to update it, then start the server again.",
                DatabaseReadiness.Describe(["20261113061457_AddSpeedBoostDash"]));
        }

        [TestMethod]
        public void SeveralMissingMigrationsAreCountedAndTheFirstIsNamed()
        {
            Assert.AreEqual(
                "The database is 2 migrations behind this build (next: First). " +
                "Run the parser to update it, then start the server again.",
                DatabaseReadiness.Describe(["First", "Second"]));
        }

        [TestMethod]
        public void AnImportOfThisVersionHasNothingToSay()
        {
            Assert.IsNull(DatabaseReadiness.DescribeGameData(DatabaseReadiness.GameDataVersion));
        }

        [TestMethod]
        public void ADatabaseNeverImportedByThisParserIsToldToRunIt()
        {
            Assert.AreEqual(
                "The game data has not been imported by this version of the parser. " +
                "Run the parser, then start the server again.",
                DatabaseReadiness.DescribeGameData(null));
        }

        [TestMethod]
        public void AnOlderImportNamesBothVersions()
        {
            Assert.AreEqual(
                $"The game data was imported by an older parser (data version 0, this build needs {DatabaseReadiness.GameDataVersion}). " +
                "Run the parser again, then start the server again.",
                DatabaseReadiness.DescribeGameData(0));
        }
    }
}
