//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NosCore.Database.Tests
{
    [TestClass]
    public class MigrationTests
    {
        [TestMethod]
        public void EveryModelChangeHasItsMigration()
        {
            using var context = new NosCoreContext(
                new DbContextOptionsBuilder<NosCoreContext>().UseNosCoreDatabase("Host=localhost").Options);

            Assert.IsFalse(context.Database.HasPendingModelChanges(),
                "The model has changes no migration records. Add one with `dotnet ef migrations add <Name> --project src/NosCore.Database`.");
        }
    }
}
