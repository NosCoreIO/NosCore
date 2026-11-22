//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.StaticEntities;
using NosCore.Database.Entities;
using NosCore.Parser.Parsers;
using NosCore.Tests.Shared;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class ScriptParserTests
    {
        private TestWorld World;
        private string _folder;
        private IDao<ScriptDto, Guid> _scripts;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _scripts = World.Create<Dao<Script, ScriptDto, Guid>>();
            World.Inject(_scripts);
            _folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_folder);
        }

        [TestCleanup]
        public void Cleanup() => Directory.Delete(_folder, true);

        private async Task ImportAsync(string tutorial)
        {
            await File.WriteAllTextAsync(Path.Combine(_folder, "tutorial.dat"), tutorial);
            await World.Create<ScriptParser>().InsertScriptsAsync(_folder);
        }

        [TestMethod]
        public async Task ASecondImportUpdatesEachStepInPlace()
        {
            await ImportAsync("script 1\r\n10\ttalk 9000\r\n25\tquest 1997\r\n");
            var ids = _scripts.LoadAll().ToDictionary(s => s.ScriptStepId, s => s.Id);

            await ImportAsync("script 1\r\n10\ttalk 9001\r\n25\tquest 1997\r\n");

            var stored = _scripts.LoadAll().ToList();
            Assert.AreEqual(2, stored.Count);
            Assert.AreEqual(ids[10], stored.Single(s => s.ScriptStepId == 10).Id);
            Assert.AreEqual((short)9001, stored.Single(s => s.ScriptStepId == 10).Argument1);
        }

        [TestMethod]
        public async Task ARunStepWithAKnownDestinationIsStoredWithIt()
        {
            await ImportAsync("script 1\r\n474\trun 200\r\n500\trun 9\r\n510\ttalk 200\r\n");

            var stored = _scripts.LoadAll().ToDictionary(s => s.ScriptStepId);
            Assert.AreEqual(((short?)1, (short?)81, (short?)33), (stored[474].TeleportMapId, stored[474].TeleportX, stored[474].TeleportY));
            Assert.IsNull(stored[500].TeleportMapId);
            Assert.IsNull(stored[510].TeleportMapId);
        }
    }
}
