//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.I18N;
using NosCore.Parser.Parsers;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class I18NParserTests
    {
        private string _folder;
        private readonly List<I18NActDescDto> _existing = new();
        private readonly List<I18NActDescDto> _saved = new();
        private I18NParser<I18NActDescDto, int> _parser;

        [TestInitialize]
        public void Setup()
        {
            _folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_folder);
            var dao = new Mock<IDao<I18NActDescDto, int>>();
            dao.Setup(s => s.LoadAll()).Returns(_existing);
            dao.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<I18NActDescDto>>()))
                .Callback<IEnumerable<I18NActDescDto>>(rows =>
                {
                    lock (_saved)
                    {
                        _saved.AddRange(rows);
                    }
                })
                .ReturnsAsync(true);
            _parser = new I18NParser<I18NActDescDto, int>(dao.Object, NullLogger<I18NParser<I18NActDescDto, int>>.Instance,
                new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object);
        }

        [TestCleanup]
        public void Cleanup() => Directory.Delete(_folder, true);

        private void WriteFile(string regionCode, params string[] lines) =>
            File.WriteAllText(Path.Combine(_folder, $"_code_{regionCode}_act_desc.txt"), string.Join("\r\n", lines));

        private Task ParseAsync() =>
            _parser.InsertI18NAsync(Path.Combine(_folder, "_code_{0}_act_desc.txt"), LogLanguageKey.I18N_ACTDESC_PARSED);

        [TestMethod]
        public async Task EnglishIsReadFromTheUkFileAndCzechFromTheCzFile()
        {
            WriteFile("uk", "zts1e\tHello");
            WriteFile("cz", "zts1e\tAhoj");

            await ParseAsync();

            Assert.AreEqual("Hello", _saved.Single(s => s.RegionType == RegionType.EN).Text);
            Assert.AreEqual("Ahoj", _saved.Single(s => s.RegionType == RegionType.CS).Text);
        }

        [TestMethod]
        public async Task TheFirstLineOfARepeatedKeyWins()
        {
            WriteFile("fr", "zts1e\tBonjour", "zts1e\tSalut", "zts2e\tAu revoir");

            await ParseAsync();

            CollectionAssert.AreEquivalent(new[] { ("zts1e", "Bonjour"), ("zts2e", "Au revoir") },
                _saved.Select(s => (s.Key, s.Text)).ToList());
        }

        [TestMethod]
        public async Task KeysAlreadyStoredForTheRegionAreSkipped()
        {
            _existing.Add(new I18NActDescDto { Key = "zts1e", RegionType = RegionType.DE, Text = "Hallo" });
            WriteFile("de", "zts1e\tHallo", "zts2e\tTschuss");

            await ParseAsync();

            Assert.AreEqual("zts2e", _saved.Single().Key);
        }

        [TestMethod]
        public async Task LinesWithoutATabAreIgnored()
        {
            WriteFile("uk", "header only", "", "zts1e\tHello");

            await ParseAsync();

            Assert.AreEqual("zts1e", _saved.Single().Key);
        }

        [TestMethod]
        public async Task MissingLanguageFilesAreSkipped()
        {
            await ParseAsync();

            Assert.AreEqual(0, _saved.Count);
        }
    }
}
