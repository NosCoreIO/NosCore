//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Database;
using NosCore.Database.Entities;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.Persistence.Tests
{
    [TestClass]
    public class QuestDuplicateMigrationTests
    {
        private const string Migration = "UniqueQuestObjectivesAndPrizes";
        private const short QuestId = 1510;

        private static readonly Guid Kept = Guid.Parse("00000000-0000-0000-0000-000000000001");
        private static readonly Guid FirstCopy = Guid.Parse("00000000-0000-0000-0000-000000000002");
        private static readonly Guid SecondCopy = Guid.Parse("00000000-0000-0000-0000-000000000003");
        private static readonly Guid CharacterQuestId = Guid.NewGuid();

        private PostgresDatabase _database;
        private NosCoreContext _context;
        private IMigrator _migrator;

        [TestInitialize]
        public async Task SetupAsync()
        {
            _database = await PostgresDatabase.CreateEmptyAsync();
            _context = _database.CreateContext();
            _migrator = _context.GetService<IMigrator>();
        }

        [TestCleanup]
        public async Task CleanupAsync()
        {
            if (_database is { } database)
            {
                await _context.DisposeAsync();
                await database.DisposeAsync();
            }
        }

        [TestMethod]
        public async Task ObjectivesAndPrizesImportedMoreThanOnceAreMergedIntoOne()
        {
            await new Spec("A quest whose objective and prize were imported three times keeps one of each, with the progress on them")
                .GivenAsync(ADatabaseBeforeTheMigration)
                .AndAsync(AQuestImportedThreeTimesWithProgressOnTwoCopies)
                .WhenAsync(Migrating)
                .ThenAsync(OneObjectiveAndOnePrizeRemain)
                .AndAsync(TheHighestProgressPointsAtTheRemainingObjective)
                .ExecuteAsync();
        }

        private Task ADatabaseBeforeTheMigration()
        {
            var migrations = _context.Database.GetMigrations().ToList();
            return _migrator.MigrateAsync(migrations[migrations.FindIndex(m => m.EndsWith("_" + Migration)) - 1]);
        }

        private async Task AQuestImportedThreeTimesWithProgressOnTwoCopies()
        {
            await using var context = _database.CreateContext();
            context.Add(new Account { AccountId = 1, Name = "account", Password = "password" });
            context.Add(new Map { MapId = 1, Name = "map", Data = [] });
            context.Add(new Character { CharacterId = 1, AccountId = 1, MapId = 1, Name = "character" });
            context.Add(new QuestReward { QuestRewardId = 4 });
            context.Add(new Quest { QuestId = QuestId, Title = "title", Desc = "desc" });
            foreach (var id in new[] { SecondCopy, Kept, FirstCopy })
            {
                context.Add(new QuestObjective { QuestObjectiveId = id, QuestId = QuestId, FirstData = 350 });
                context.Add(new QuestQuestReward { Id = Guid.NewGuid(), QuestId = QuestId, QuestRewardId = 4 });
            }

            context.Add(new CharacterQuest { Id = CharacterQuestId, CharacterId = 1, QuestId = QuestId });
            context.Add(new CharacterQuestObjective { Id = Guid.NewGuid(), CharacterQuestId = CharacterQuestId, QuestObjectiveId = FirstCopy, Count = 1 });
            context.Add(new CharacterQuestObjective { Id = Guid.NewGuid(), CharacterQuestId = CharacterQuestId, QuestObjectiveId = SecondCopy, Count = 3 });
            await context.SaveChangesAsync();
        }

        private Task Migrating() => _migrator.MigrateAsync();

        private async Task OneObjectiveAndOnePrizeRemain()
        {
            await using var context = _database.CreateContext();
            CollectionAssert.AreEqual(new[] { Kept }, await context.Set<QuestObjective>().Select(o => o.QuestObjectiveId).ToArrayAsync());
            Assert.AreEqual(1, await context.Set<QuestQuestReward>().CountAsync());
        }

        private async Task TheHighestProgressPointsAtTheRemainingObjective()
        {
            await using var context = _database.CreateContext();
            var progress = await context.Set<CharacterQuestObjective>().SingleAsync();
            Assert.AreEqual(Kept, progress.QuestObjectiveId);
            Assert.AreEqual(3, progress.Count);
        }
    }
}
