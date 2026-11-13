//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.EndToEnd.Tests.Infrastructure;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class StartupTests
    {
        [TestMethod]
        [DataRow("NosCore.MasterServer")]
        [DataRow("NosCore.LoginServer")]
        [DataRow("NosCore.WorldServer")]
        public async Task AServerOnADatabaseThatIsBehindSaysSoInsteadOfStarting(string server)
        {
            _ = Harness.Stack;
            await using var behind = await TestDatabase.TryCreateAsync(migrationsBehind: 1)
                ?? throw new InvalidOperationException("PostgreSQL stopped answering.");

            var written = await ServerStack.StartAloneAsync(server, behind.Name, "migration behind this build");

            var line = written.Single(l => l.Contains("migration behind this build", StringComparison.Ordinal));
            StringAssert.StartsWith(line, "The database is 1 migration behind this build (next: ");
            StringAssert.EndsWith(line, "Run the parser to update it, then start the server again.");
        }
    }
}
