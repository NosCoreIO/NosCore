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
    }
}
