//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Character;
using NosCore.Database.Entities;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.Persistence.Tests
{
    [TestClass]
    public class DaoTransactionScopeTests
    {
        private const long AccountId = 5001;
        private const long MissingAccountId = 5999;

        private PostgresDatabase _database;
        private TestWorld World;
        private IDaoTransactionScope Scope;
        private CharacterDto? _refused;

        [TestInitialize]
        public async Task SetupAsync()
        {
            _database = await PostgresDatabase.CreateAsync();
            World = await TestWorld.CreateAsync(_database);
            Scope = World.Create<IDaoTransactionScope>();
        }

        [TestCleanup]
        public async Task CleanupAsync()
        {
            if (_database is { } database)
            {
                await database.DisposeAsync();
            }
        }

        [TestMethod]
        public async Task AScopeThatIsNotCommittedLeavesNoRows()
        {
            await new Spec("A scope disposed without a commit leaves none of its writes behind")
                .WhenAsync(AnAccountIsWrittenAndTheScopeIsNotCommitted)
                .ThenAsync(TheAccountIsNotStored)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACommittedScopeKeepsItsRows()
        {
            await new Spec("A committed scope keeps its writes")
                .WhenAsync(AnAccountIsWrittenAndTheScopeIsCommitted)
                .ThenAsync(TheAccountIsStored)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AForeignKeyViolationRollsBackTheWritesBeforeIt()
        {
            await new Spec("A character pointing at a missing account is refused, and the account written before it in the scope is rolled back")
                .WhenAsync(AnAccountAndACharacterOfAMissingAccountAreWrittenInOneScope)
                .Then(TheCharacterIsRefused)
                .AndAsync(TheAccountIsNotStored)
                .ExecuteAsync();
        }

        private async Task AnAccountIsWrittenAndTheScopeIsNotCommitted()
        {
            await using var transaction = Scope.Begin();
            Assert.IsNotNull(await World.AccountDao.TryInsertOrUpdateAsync(Account()));
        }

        private async Task AnAccountIsWrittenAndTheScopeIsCommitted()
        {
            await using var transaction = Scope.Begin();
            Assert.IsNotNull(await World.AccountDao.TryInsertOrUpdateAsync(Account()));
            await transaction.CommitAsync();
        }

        private async Task AnAccountAndACharacterOfAMissingAccountAreWrittenInOneScope()
        {
            await using var transaction = Scope.Begin();
            Assert.IsNotNull(await World.AccountDao.TryInsertOrUpdateAsync(Account()));
            _refused = await World.CharacterDao.TryInsertOrUpdateAsync(new CharacterDto
            {
                CharacterId = 5002,
                AccountId = MissingAccountId,
                Name = "Orphan",
                MapId = 1,
                State = CharacterState.Active,
                Level = 1,
                JobLevel = 1
            });
        }

        private void TheCharacterIsRefused() => Assert.IsNull(_refused);

        private async Task TheAccountIsNotStored() => Assert.IsFalse(await AccountIsStoredAsync());

        private async Task TheAccountIsStored() => Assert.IsTrue(await AccountIsStoredAsync());

        private async Task<bool> AccountIsStoredAsync()
        {
            await using var context = _database.CreateContext();
            return await context.Set<Account>().AnyAsync(a => a.AccountId == AccountId);
        }

        private static AccountDto Account() => new() { AccountId = AccountId, Name = "Scoped", Password = "x" };
    }
}
