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
    public class ConstStringParserTests
    {
        // conststring_UK.dat 2081..2107 verbatim: the twenty-seven numeric reputation bands.
        private static readonly string[] ReputationBands =
        [
            "0 - 50", "51 - 150", "151 - 250", "251 - 500", "501 - 750", "751 - 1000",
            "1001 - 2250", "2251 - 3500", "3501 - 5000", "5001 - 9500", "9501 - 19000",
            "19001 - 25000", "25001 - 40000", "40001 - 60000", "60001 - 85000",
            "85001 - 115000", "115001 - 150000", "150001 - 190000", "190001 - 235000",
            "235001 - 285000", "285001 - 350000", "350001 - 500000", "500001 - 1500000",
            "1500001 - 2500000", "2500001 - 3750000", "3750001 - 5000000", "Over 5000000"
        ];

        // 2108..2110, the ranking places the import skips.
        private static readonly string[] RankBands = ["51st-100th", "21st-50th", "4th-20th"];

        // 2111..2116, effects text and all.
        private static readonly string[] DignityBands =
        [
            "100 - 0",
            "-100 ~ -200#13#10 Title changed!",
            "-201 to -400#13#1010% price increase when purchasing shop items!",
            "-401 to -600#13#10 20% price increase when purchasing shop items!#13#10You can no longer capture pets!",
            "-601 to -800#13#1050% price increase when purchasing shop items!#13#10You can no longer capture pets!",
            "-800 to -1000#13#1050% price increase when purchasing shop items!#13#10You can no longer capture pets!#13#10Your NosMate will no longer accompany you."
        ];

        private Mock<IDao<DignityLevelDto, byte>> _dignityDaoMock = null!;
        private List<DignityLevelDto> _dignitySaved = null!;
        private Mock<ILogLanguageLocalizer<LogLanguageKey>> _logLanguageMock = null!;
        private Mock<IDao<ReputationLevelDto, byte>> _reputationDaoMock = null!;
        private List<ReputationLevelDto> _reputationSaved = null!;
        private string _tempFolder = null!;

        [TestInitialize]
        public void Setup()
        {
            _logLanguageMock = new Mock<ILogLanguageLocalizer<LogLanguageKey>>();

            _reputationSaved = [];
            _reputationDaoMock = new Mock<IDao<ReputationLevelDto, byte>>();
            _reputationDaoMock
                .Setup(x => x.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<ReputationLevelDto>>()))
                .Callback<IEnumerable<ReputationLevelDto>>(r => _reputationSaved.AddRange(r))
                .ReturnsAsync(true);

            _dignitySaved = [];
            _dignityDaoMock = new Mock<IDao<DignityLevelDto, byte>>();
            _dignityDaoMock
                .Setup(x => x.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<DignityLevelDto>>()))
                .Callback<IEnumerable<DignityLevelDto>>(r => _dignitySaved.AddRange(r))
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

        private Task ImportAsync(IEnumerable<string>? reputation = null, IEnumerable<string>? dignity = null)
        {
            // Each group sits at its own base key, so dropping a band leaves a hole rather than
            // shifting every later band onto the wrong key.
            var builder = new StringBuilder();
            builder.Append("2045\vBeginner\r");
            Append(builder, 2081, reputation ?? ReputationBands);
            Append(builder, 2108, RankBands);
            Append(builder, 2111, dignity ?? DignityBands);
            builder.Append("2117\vReputation / Dignity\r");
            File.WriteAllText(Path.Combine(_tempFolder, "conststring_UK.dat"), builder.ToString(), Encoding.Latin1);

            return new ConstStringParser(_reputationDaoMock.Object, _dignityDaoMock.Object,
                NullLogger<ConstStringParser>.Instance, _logLanguageMock.Object).InsertLaddersAsync(_tempFolder);
        }

        private static void Append(StringBuilder builder, int firstKey, IEnumerable<string> bands)
        {
            foreach (var band in bands)
            {
                builder.Append(firstKey++).Append('\v').Append(band).Append('\r');
            }
        }

        [TestMethod]
        public async Task TheClientBandsImportAsTwentySevenReputationLevelsInEnumOrder()
        {
            await ImportAsync();

            Assert.AreEqual(27, _reputationSaved.Count);
            Assert.AreEqual((byte)ReputationType.GreenBeginner, _reputationSaved[0].ReputationLevelId);
            Assert.AreEqual((byte)ReputationType.RedElite, _reputationSaved[^1].ReputationLevelId);
        }

        [TestMethod]
        public async Task EachReputationLevelCarriesTheBoundsTheClientDeclares()
        {
            await ImportAsync();

            Assert.AreEqual(0, _reputationSaved[0].MinReputation);
            Assert.AreEqual(50, _reputationSaved[0].MaxReputation);
            Assert.AreEqual(51, _reputationSaved[1].MinReputation);
            Assert.AreEqual(150, _reputationSaved[1].MaxReputation);
            Assert.AreEqual(3_750_001, _reputationSaved[25].MinReputation);
            Assert.AreEqual(5_000_000, _reputationSaved[25].MaxReputation);
        }

        [TestMethod]
        public async Task TheHighestReputationBandIsOpenEndedAndStartsAfterThePreviousOne()
        {
            await ImportAsync();

            Assert.AreEqual(5_000_001, _reputationSaved[^1].MinReputation);
            Assert.IsNull(_reputationSaved[^1].MaxReputation);
        }

        [TestMethod]
        public async Task TheReputationBandsAreContiguousWithNoGapOrOverlap()
        {
            await ImportAsync();

            for (var i = 1; i < _reputationSaved.Count; i++)
            {
                Assert.AreEqual(_reputationSaved[i - 1].MaxReputation + 1, _reputationSaved[i].MinReputation,
                    $"band {i} must start one above the ceiling of band {i - 1}");
            }
        }

        [TestMethod]
        public async Task AGapBetweenTwoReputationBandsImportsNoReputation()
        {
            var gapped = ReputationBands.ToArray();
            gapped[1] = "52 - 150";

            await ImportAsync(reputation: gapped);

            Assert.AreEqual(0, _reputationSaved.Count);
        }

        [TestMethod]
        public async Task AHighestBandThatDisagreesWithThePreviousCeilingImportsNoReputation()
        {
            var mismatched = ReputationBands.ToArray();
            mismatched[^1] = "Over 6000000";

            await ImportAsync(reputation: mismatched);

            Assert.AreEqual(0, _reputationSaved.Count);
        }

        [TestMethod]
        public async Task AReputationBandThatStoppedBeingNumericImportsNoReputation()
        {
            // Index 1 wants exactly 51 and 150, so only the suffix can reject this. At index 20
            // the bounds reject it first and the test goes green over a parser that never looked.
            var ranked = ReputationBands.ToArray();
            ranked[1] = "51st-150th";

            await ImportAsync(reputation: ranked);

            Assert.AreEqual(0, _reputationSaved.Count);
        }

        [TestMethod]
        public async Task ARankingBandWrittenWithBareNumbersImportsNoReputation()
        {
            // French and German write ranking places with bare numbers, so a rule about suffixes
            // lets them through. What holds in all eight languages: no numeric band has a letter.
            var ranked = ReputationBands.ToArray();
            ranked[1] = "Place 51 a 150";

            await ImportAsync(reputation: ranked);

            Assert.AreEqual(0, _reputationSaved.Count);
        }

        [TestMethod]
        public async Task AReputationNumberTooLargeForALongImportsNoReputation()
        {
            var overflowing = ReputationBands.ToArray();
            overflowing[1] = "51 - 99999999999999999999";

            await ImportAsync(reputation: overflowing);

            Assert.AreEqual(0, _reputationSaved.Count);
        }

        [TestMethod]
        public async Task TheClientBandsImportAsSixDignityLevelsInEnumOrder()
        {
            await ImportAsync();

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6 },
                _dignitySaved.Select(level => (int)level.DignityLevelId).ToArray());
        }

        [TestMethod]
        public async Task DefaultCatchesEverythingAboveTheFirstPenaltyBand()
        {
            await ImportAsync();

            // The client declares nothing between -1 and -99, so Default cannot carry a ceiling.
            Assert.AreEqual((byte)DignityType.Default, _dignitySaved[0].DignityLevelId);
            Assert.IsNull(_dignitySaved[0].MaxDignity);
        }

        [TestMethod]
        public async Task EachPenaltyBandTakesTheCeilingTheClientDeclares()
        {
            await ImportAsync();

            Assert.AreEqual((short)-100, _dignitySaved[1].MaxDignity);
            Assert.AreEqual((short)-201, _dignitySaved[2].MaxDignity);
            Assert.AreEqual((short)-401, _dignitySaved[3].MaxDignity);
            Assert.AreEqual((short)-601, _dignitySaved[4].MaxDignity);
        }

        [TestMethod]
        public async Task TheClientsOverlappingLastBandIsResolvedToMinusEightHundredAndOne()
        {
            await ImportAsync();

            // Useless ends at -800 and Failed is declared "-800 to -1000", putting -800 in two
            // bands. Deriving from the previous floor gives the -801 the packet docs state.
            Assert.AreEqual((short)-801, _dignitySaved[5].MaxDignity);
        }

        [TestMethod]
        public async Task TheEffectsTextIsNotReadAsBandNumbers()
        {
            await ImportAsync();

            // The escape tail carries 13, 1010 and 10.
            Assert.AreEqual((short)-201, _dignitySaved[2].MaxDignity);
        }

        [TestMethod]
        public async Task ADignityBandWhoseFloorRisesInsteadOfFallingImportsNoDignity()
        {
            var wrong = DignityBands.ToArray();
            wrong[3] = "-401 to -150";

            await ImportAsync(dignity: wrong);

            Assert.AreEqual(0, _dignitySaved.Count);
        }

        [TestMethod]
        public async Task ADignityBandWithoutTwoNumbersImportsNoDignity()
        {
            var wrong = DignityBands.ToArray();
            wrong[2] = "no longer a range#13#10Title changed!";

            await ImportAsync(dignity: wrong);

            Assert.AreEqual(0, _dignitySaved.Count);
        }

        [TestMethod]
        public async Task ADignityNumberTooLargeForAShortImportsNoDignity()
        {
            var overflowing = DignityBands.ToArray();
            overflowing[1] = "-100 ~ -40000#13#10 Title changed!";

            await ImportAsync(dignity: overflowing);

            Assert.AreEqual(0, _dignitySaved.Count);
        }

        [TestMethod]
        public async Task AMissingBandImportsNothingForThatLadderOnly()
        {
            await ImportAsync(reputation: ReputationBands.Take(26));

            Assert.AreEqual(0, _reputationSaved.Count);
            Assert.AreEqual(6, _dignitySaved.Count);
        }

        [TestMethod]
        public async Task AMalformedReputationTableStillImportsDignity()
        {
            var gapped = ReputationBands.ToArray();
            gapped[1] = "52 - 150";

            await ImportAsync(reputation: gapped);

            Assert.AreEqual(0, _reputationSaved.Count);
            Assert.AreEqual(6, _dignitySaved.Count);
        }

        [TestMethod]
        public async Task AMalformedDignityTableStillImportsReputation()
        {
            var wrong = DignityBands.ToArray();
            wrong[3] = "-401 to -150";

            await ImportAsync(dignity: wrong);

            Assert.AreEqual(0, _dignitySaved.Count);
            Assert.AreEqual(27, _reputationSaved.Count);
        }
    }
}
