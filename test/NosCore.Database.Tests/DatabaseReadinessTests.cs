//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Database.Entities;
using System;
using System.IO;

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
                "Run the parser to update it.",
                DatabaseReadiness.Describe(["20261113061457_AddSpeedBoostDash"]));
        }

        [TestMethod]
        public void SeveralMissingMigrationsAreCountedAndTheFirstIsNamed()
        {
            Assert.AreEqual(
                "The database is 2 migrations behind this build (next: First). " +
                "Run the parser to update it.",
                DatabaseReadiness.Describe(["First", "Second"]));
        }

        private const string ParserBuild = "BUILD";

        [TestMethod]
        public void AnImportByThisParserBuildFromTheSameFilesHasNothingToSay()
        {
            var folder = ClientFolder();
            Assert.IsNull(DatabaseReadiness.DescribeGameData(Import(folder), ParserBuild));
        }

        [TestMethod]
        public void ADatabaseNeverImportedIsToldToRunTheParser()
        {
            Assert.AreEqual(
                "The game data has not been imported. Run the parser.",
                DatabaseReadiness.DescribeGameData(null, ParserBuild));
        }

        [TestMethod]
        public void ChangedClientFilesMakeTheImportStale()
        {
            var folder = ClientFolder();
            var import = Import(folder);
            File.WriteAllText(Path.Combine(folder, "Item.dat"), "changed");
            Assert.AreEqual(
                $"The game data could not be imported again from {folder}. Run the parser to see why.",
                DatabaseReadiness.DescribeGameData(import, ParserBuild));
        }

        [TestMethod]
        public void AnotherParserBuildMakesTheImportStale()
        {
            var folder = ClientFolder();
            Assert.IsNotNull(DatabaseReadiness.DescribeGameData(Import(folder), "OTHER"));
        }

        [TestMethod]
        public void AnUnreadableFolderIsOnlyCheckedAgainstTheParserBuild()
        {
            var import = Import(ClientFolder());
            import.Folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Assert.IsNull(DatabaseReadiness.DescribeGameData(import, ParserBuild));
            Assert.AreEqual(
                $"The game data was imported by another build of the parser, from {import.Folder}, which this server cannot read. " +
                "Run the parser again.",
                DatabaseReadiness.DescribeGameData(import, "OTHER"));
        }

        [TestMethod]
        public void TheParserBuildCoversTheNosCoreAssembliesItRuns()
        {
            Assert.AreEqual(
                GameDataFingerprint.OfParser(typeof(DatabaseReadiness).Assembly),
                GameDataFingerprint.OfParser(typeof(DatabaseReadiness).Assembly));
            Assert.AreNotEqual(
                GameDataFingerprint.OfParser(typeof(DatabaseReadiness).Assembly),
                GameDataFingerprint.OfParser(typeof(DatabaseReadinessTests).Assembly));
        }

        private static string ClientFolder()
        {
            var folder = Path.Combine(Path.GetTempPath(), "noscore-client-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(folder, "map"));
            File.WriteAllText(Path.Combine(folder, "Item.dat"), "item");
            File.WriteAllText(Path.Combine(folder, "map", "1"), "map");
            return folder;
        }

        private static GameDataImport Import(string folder) => new()
        {
            GameDataImportId = 1,
            ParserBuild = ParserBuild,
            ClientFiles = GameDataFingerprint.OfClientFiles(folder),
            Folder = folder,
        };
    }
}
