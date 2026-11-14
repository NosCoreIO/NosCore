//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Parser.Parsers;
using NosCore.Shared.I18N;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class ScriptOfferParserTests
    {
        private string _folder;
        private List<ScriptOfferDto> _saved;
        private ScriptOfferParser _parser;

        [TestInitialize]
        public void Setup()
        {
            _folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_folder);
            _saved = new List<ScriptOfferDto>();
            var dao = new Mock<IDao<ScriptOfferDto, byte>>();
            dao.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<ScriptOfferDto>>()))
                .Callback<IEnumerable<ScriptOfferDto>>(rows => _saved.AddRange(rows))
                .ReturnsAsync(true);
            _parser = new ScriptOfferParser(dao.Object, NullLogger<ScriptOfferParser>.Instance,
                new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object);
            File.WriteAllText(Path.Combine(_folder, "qstnpc.dat"), string.Join("\r",
                "#=== story chapter 2",
                "331\t0\t2\t1\t19\t1",
                "#=== story chapter 13",
                "394\t0\t13\t12\t57\t88",
                "#=== daily quest",
                "2541\t1\t7516\t0\t89",
                "~1"));
            File.WriteAllText(Path.Combine(_folder, "npctalk.dat"), string.Join("\r",
                "% 1",
                "% c",
                "b zts4759e  . n_run 3000 0",
                "% 42",
                "b zts4971e  . n_run 3001 2",
                "% 52",
                "b zts5698e  . n_run 3001 12",
                "b zts5699e  . n_run 3001 13"));
        }

        [TestCleanup]
        public void Cleanup() => Directory.Delete(_folder, true);

        [TestMethod]
        public async Task StoryChaptersAreReadWithTheDialogThatListsThem()
        {
            await _parser.InsertScriptOffersAsync(_folder);

            Assert.AreEqual("2:331:1:19:42 13:394:12:57:52", string.Join(" ", _saved.OrderBy(o => o.ScriptId)
                .Select(o => $"{o.ScriptId}:{o.NpcVNum}:{o.RequiredScriptId}:{o.LevelMinimum}:{o.DialogId}")));
        }
    }
}
