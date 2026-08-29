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
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class DignityLevelParserTests
    {
        // conststring_UK.dat 2111..2116 verbatim, effects text and all.
        private static readonly string[] ClientBands =
        [
            "100 - 0",
            "-100 ~ -200#13#10 Title changed!",
            "-201 to -400#13#1010% price increase when purchasing shop items!",
            "-401 to -600#13#10 20% price increase when purchasing shop items!#13#10You can no longer capture pets!",
            "-601 to -800#13#1050% price increase when purchasing shop items!#13#10You can no longer capture pets!",
            "-800 to -1000#13#1050% price increase when purchasing shop items!#13#10You can no longer capture pets!#13#10Your NosMate will no longer accompany you."
        ];

        private Mock<IDao<DignityLevelDto, byte>> _daoMock = null!;
        private Mock<ILogLanguageLocalizer<LogLanguageKey>> _logLanguageMock = null!;
        private List<DignityLevelDto> _saved = null!;
        private string _tempFolder = null!;

        [TestInitialize]
        public void Setup()
        {
            _logLanguageMock = new Mock<ILogLanguageLocalizer<LogLanguageKey>>();
            _daoMock = new Mock<IDao<DignityLevelDto, byte>>();
            _saved = [];
            _daoMock
                .Setup(x => x.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<DignityLevelDto>>()))
                .Callback<IEnumerable<DignityLevelDto>>(r => _saved.AddRange(r))
                .ReturnsAsync(true);

            _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempFolder);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_tempFolder))
            {
                Directory.Delete(_tempFolder, true);
            }
        }

        private DignityLevelParser Parser() =>
            new(_daoMock.Object, NullLogger<DignityLevelParser>.Instance, _logLanguageMock.Object);

        private void WriteConstString(IEnumerable<string> bands, int firstKey = 2111)
        {
            var builder = new StringBuilder();
            builder.Append("2110\v4th-20th\r");
            var key = firstKey;
            foreach (var band in bands)
            {
                builder.Append(key++).Append('\v').Append(band).Append('\r');
            }
            builder.Append("2117\vReputation / Dignity\r");
            File.WriteAllText(Path.Combine(_tempFolder, "conststring_UK.dat"), builder.ToString(), Encoding.Latin1);
        }

        [TestMethod]
        public async Task TheClientBandsImportAsSixLevelsInEnumOrder()
        {
            WriteConstString(ClientBands);

            await Parser().InsertDignityLevelsAsync(_tempFolder);

            Assert.AreEqual(6, _saved.Count);
            CollectionAssert.AreEqual(
                new[] { 1, 2, 3, 4, 5, 6 },
                _saved.Select(level => (int)level.DignityLevelId).ToArray());
        }

        [TestMethod]
        public async Task DefaultCatchesEverythingAboveTheFirstPenaltyBand()
        {
            WriteConstString(ClientBands);

            await Parser().InsertDignityLevelsAsync(_tempFolder);

            // The client declares nothing between -1 and -99, so Default cannot carry a ceiling.
            Assert.AreEqual((byte)DignityType.Default, _saved[0].DignityLevelId);
            Assert.IsNull(_saved[0].MaxDignity);
        }

        [TestMethod]
        public async Task EachPenaltyBandTakesTheCeilingTheClientDeclares()
        {
            WriteConstString(ClientBands);

            await Parser().InsertDignityLevelsAsync(_tempFolder);

            Assert.AreEqual((short)-100, _saved[1].MaxDignity);
            Assert.AreEqual((short)-201, _saved[2].MaxDignity);
            Assert.AreEqual((short)-401, _saved[3].MaxDignity);
            Assert.AreEqual((short)-601, _saved[4].MaxDignity);
        }

        [TestMethod]
        public async Task TheClientsOverlappingLastBandIsResolvedToMinusEightHundredAndOne()
        {
            WriteConstString(ClientBands);

            await Parser().InsertDignityLevelsAsync(_tempFolder);

            // Useless ends at -800 and Failed is declared "-800 to -1000", putting -800 in two
            // bands. Deriving from the previous floor gives the -801 the packet docs state.
            Assert.AreEqual((short)-801, _saved[5].MaxDignity);
        }

        [TestMethod]
        public async Task TheEffectsTextIsNotReadAsBandNumbers()
        {
            WriteConstString(ClientBands);

            await Parser().InsertDignityLevelsAsync(_tempFolder);

            // The escape tail carries 13, 1010 and 10.
            Assert.AreEqual((short)-201, _saved[2].MaxDignity);
        }

        [TestMethod]
        public async Task ABandWhoseFloorRisesInsteadOfFallingImportsNothing()
        {
            var wrong = ClientBands.ToArray();
            wrong[3] = "-401 to -150";

            WriteConstString(wrong);

            await Parser().InsertDignityLevelsAsync(_tempFolder);

            Assert.AreEqual(0, _saved.Count);
        }

        [TestMethod]
        public async Task ABandNumberTooLargeForAShortImportsNothing()
        {
            var overflowing = ClientBands.ToArray();
            overflowing[1] = "-100 ~ -40000#13#10 Title changed!";

            WriteConstString(overflowing);

            await Parser().InsertDignityLevelsAsync(_tempFolder);

            Assert.AreEqual(0, _saved.Count);
        }

        [TestMethod]
        public async Task AMissingBandImportsNothing()
        {
            WriteConstString(ClientBands.Take(5));

            await Parser().InsertDignityLevelsAsync(_tempFolder);

            Assert.AreEqual(0, _saved.Count);
        }

        [TestMethod]
        public async Task ABandWithoutTwoNumbersImportsNothing()
        {
            var wrong = ClientBands.ToArray();
            wrong[2] = "no longer a range#13#10Title changed!";

            WriteConstString(wrong);

            await Parser().InsertDignityLevelsAsync(_tempFolder);

            Assert.AreEqual(0, _saved.Count);
        }
    }
}
