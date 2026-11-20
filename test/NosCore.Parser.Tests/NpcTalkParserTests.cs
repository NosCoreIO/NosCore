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
using NosCore.Packets.Enumerations;
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
    public class NpcTalkParserTests
    {
        private readonly string _folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        private readonly List<NpcTalkButtonDto> _buttons = [];
        private readonly NpcTalkParser _parser;

        public NpcTalkParserTests()
        {
            var talks = new Mock<IDao<NpcTalkDto, short>>();
            talks.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<NpcTalkDto>>())).ReturnsAsync(true);
            var buttons = new Mock<IDao<NpcTalkButtonDto, Guid>>();
            buttons.Setup(s => s.LoadAll()).Returns(() => _buttons.ToList());
            buttons.Setup(s => s.TryDeleteAsync(It.IsAny<IEnumerable<Guid>>()))
                .Returns((IEnumerable<Guid> ids) =>
                {
                    var deleted = _buttons.Where(b => ids.Contains(b.NpcTalkButtonId)).ToList();
                    _buttons.RemoveAll(deleted.Contains);
                    return Task.FromResult<IEnumerable<NpcTalkButtonDto>?>(deleted);
                });
            buttons.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<NpcTalkButtonDto>>()))
                .Callback<IEnumerable<NpcTalkButtonDto>>(rows =>
                {
                    foreach (var row in rows)
                    {
                        _buttons.RemoveAll(b => b.NpcTalkButtonId == row.NpcTalkButtonId);
                        _buttons.Add(row);
                    }
                })
                .ReturnsAsync(true);
            _parser = new NpcTalkParser(talks.Object, buttons.Object, NullLoggerFactory.Instance,
                new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object);
        }

        [TestInitialize]
        public void Setup()
        {
            Directory.CreateDirectory(_folder);
            File.WriteAllText(Path.Combine(_folder, "npctalk.dat"), string.Join("\r",
                "# npc talk",
                "% 1",
                "t zts1e",
                "s 0",
                "b zts3677e  . shopping 3 0",
                "b zts3678e  . n_talk 54 0&@",
                "b zts7087e  . n_run 14 0",
                "% 54",
                "t zts54e",
                "b zts5966e  . n_run 3002 0",
                "b zts5967e  . n_run 3002 1",
                "b zts5967e  . n_run 3002 1",
                "b zts5968e  . n_run 9999 0",
                "% 9700",
                "t zts9700e",
                "b zts18090e  . n_run 201 0&@KR T_KR"));
        }

        [TestCleanup]
        public void Cleanup() => Directory.Delete(_folder, true);

        private string Buttons() => string.Join(" ", _buttons
            .OrderBy(b => b.DialogId).ThenBy(b => b.NextDialogId).ThenBy(b => b.Runner).ThenBy(b => b.Argument)
            .Select(b => b.NextDialogId is { } next ? $"{b.DialogId}>{next}" : $"{b.DialogId}:{(short?)b.Runner}:{b.Argument}"));

        [TestMethod]
        public async Task EachDialogKeepsItsRunAndTalkButtonsOnce()
        {
            await _parser.ParseAsync(_folder);

            Assert.AreEqual("1:14:0 1>54 54:3002:0 54:3002:1 9700:201:0", Buttons());
        }

        [TestMethod]
        public async Task ParsingAgainKeepsEveryButtonsId()
        {
            await _parser.ParseAsync(_folder);
            var ids = _buttons.Select(b => b.NpcTalkButtonId).ToHashSet();

            await _parser.ParseAsync(_folder);

            Assert.IsTrue(ids.SetEquals(_buttons.Select(b => b.NpcTalkButtonId)));
        }

        [TestMethod]
        public async Task AButtonNoLongerInTheFileIsRemoved()
        {
            _buttons.Add(new NpcTalkButtonDto { NpcTalkButtonId = Guid.NewGuid(), DialogId = 54, Runner = NrunRunnerType.GetQuestHigherTimeSpaceScore, Argument = 7 });

            await _parser.ParseAsync(_folder);

            Assert.IsFalse(_buttons.Any(b => b.Argument == 7));
        }
    }
}
