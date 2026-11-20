//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Database;
using NosCore.Database.Entities;
using NosCore.Parser.Parsers;
using NosCore.Shared.I18N;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class ScriptParserTests
    {
        private string _folder;
        private IDao<ScriptDto, Guid> _scripts;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            _scripts = new Dao<Script, ScriptDto, Guid>(NullLogger<Dao<Script, ScriptDto, Guid>>.Instance, () => new NosCoreContext(options));
            _folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_folder);
        }

        [TestCleanup]
        public void Cleanup() => Directory.Delete(_folder, true);

        private async Task ImportAsync(string tutorial)
        {
            await File.WriteAllTextAsync(Path.Combine(_folder, "tutorial.dat"), tutorial);
            await new ScriptParser(_scripts, new Mock<ILogger<ScriptParser>>().Object,
                new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object).InsertScriptsAsync(_folder);
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
    }
}
