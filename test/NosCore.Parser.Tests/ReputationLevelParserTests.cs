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
    public class ReputationLevelParserTests
    {
        // conststring_UK.dat 2081..2107 verbatim: the twenty-seven numeric reputation bands.
        private static readonly string[] ClientBands =
        [
            "0 - 50", "51 - 150", "151 - 250", "251 - 500", "501 - 750", "751 - 1000",
            "1001 - 2250", "2251 - 3500", "3501 - 5000", "5001 - 9500", "9501 - 19000",
            "19001 - 25000", "25001 - 40000", "40001 - 60000", "60001 - 85000",
            "85001 - 115000", "115001 - 150000", "150001 - 190000", "190001 - 235000",
            "235001 - 285000", "285001 - 350000", "350001 - 500000", "500001 - 1500000",
            "1500001 - 2500000", "2500001 - 3750000", "3750001 - 5000000", "Over 5000000"
        ];

        private Mock<IDao<ReputationLevelDto, byte>> _daoMock = null!;
        private Mock<ILogLanguageLocalizer<LogLanguageKey>> _logLanguageMock = null!;
        private List<ReputationLevelDto> _saved = null!;
        private string _tempFolder = null!;

        [TestInitialize]
        public void Setup()
        {
            _logLanguageMock = new Mock<ILogLanguageLocalizer<LogLanguageKey>>();
            _daoMock = new Mock<IDao<ReputationLevelDto, byte>>();
            _saved = [];
            _daoMock
                .Setup(x => x.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<ReputationLevelDto>>()))
                .Callback<IEnumerable<ReputationLevelDto>>(r => _saved.AddRange(r))
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

        private ReputationLevelParser Parser() =>
            new(_daoMock.Object, NullLogger<ReputationLevelParser>.Instance, _logLanguageMock.Object);

        private void WriteConstString(IEnumerable<string> bands, int firstKey = 2081)
        {
            var builder = new StringBuilder();
            builder.Append("2044\vAdd\r");
            builder.Append("2045\vBeginner\r");
            var key = firstKey;
            foreach (var band in bands)
            {
                builder.Append(key++).Append('\v').Append(band).Append('\r');
            }
            builder.Append("2117\vReputation / Dignity\r");
            File.WriteAllText(Path.Combine(_tempFolder, "conststring_UK.dat"), builder.ToString(), Encoding.Latin1);
        }

        [TestMethod]
        public async Task TheClientBandsImportAsTwentySevenLevelsInEnumOrder()
        {
            WriteConstString(ClientBands);

            await Parser().InsertReputationLevelsAsync(_tempFolder);

            Assert.AreEqual(27, _saved.Count);
            Assert.AreEqual((byte)ReputationType.GreenBeginner, _saved[0].ReputationLevelId);
            Assert.AreEqual((byte)ReputationType.RedElite, _saved[^1].ReputationLevelId);
        }

        [TestMethod]
        public async Task EachLevelCarriesTheBoundsTheClientDeclares()
        {
            WriteConstString(ClientBands);

            await Parser().InsertReputationLevelsAsync(_tempFolder);

            Assert.AreEqual(0, _saved[0].MinReputation);
            Assert.AreEqual(50, _saved[0].MaxReputation);
            Assert.AreEqual(51, _saved[1].MinReputation);
            Assert.AreEqual(150, _saved[1].MaxReputation);
            Assert.AreEqual(3_750_001, _saved[25].MinReputation);
            Assert.AreEqual(5_000_000, _saved[25].MaxReputation);
        }

        [TestMethod]
        public async Task TheHighestBandIsOpenEndedAndStartsAfterThePreviousOne()
        {
            WriteConstString(ClientBands);

            await Parser().InsertReputationLevelsAsync(_tempFolder);

            Assert.AreEqual(5_000_001, _saved[^1].MinReputation);
            Assert.IsNull(_saved[^1].MaxReputation);
        }

        [TestMethod]
        public async Task TheBandsAreContiguousWithNoGapOrOverlap()
        {
            WriteConstString(ClientBands);

            await Parser().InsertReputationLevelsAsync(_tempFolder);

            for (var i = 1; i < _saved.Count; i++)
            {
                Assert.AreEqual(_saved[i - 1].MaxReputation + 1, _saved[i].MinReputation,
                    $"band {i} must start one above the ceiling of band {i - 1}");
            }
        }

        [TestMethod]
        public async Task AGapBetweenTwoBandsImportsNothing()
        {
            var gapped = ClientBands.ToArray();
            gapped[1] = "52 - 150";

            WriteConstString(gapped);

            await Parser().InsertReputationLevelsAsync(_tempFolder);

            Assert.AreEqual(0, _saved.Count);
        }

        [TestMethod]
        public async Task AHighestBandThatDisagreesWithThePreviousCeilingImportsNothing()
        {
            var mismatched = ClientBands.ToArray();
            mismatched[^1] = "Over 6000000";

            WriteConstString(mismatched);

            await Parser().InsertReputationLevelsAsync(_tempFolder);

            Assert.AreEqual(0, _saved.Count);
        }

        [TestMethod]
        public async Task AMissingBandImportsNothing()
        {
            WriteConstString(ClientBands.Take(26));

            await Parser().InsertReputationLevelsAsync(_tempFolder);

            Assert.AreEqual(0, _saved.Count);
        }

        [TestMethod]
        public async Task ABandThatStoppedBeingNumericImportsNothing()
        {
            // 2108 onwards are ranking places, not thresholds.
            var ranked = ClientBands.ToArray();
            ranked[20] = "51st-100th";

            WriteConstString(ranked);

            await Parser().InsertReputationLevelsAsync(_tempFolder);

            Assert.AreEqual(0, _saved.Count);
        }
    }
}
