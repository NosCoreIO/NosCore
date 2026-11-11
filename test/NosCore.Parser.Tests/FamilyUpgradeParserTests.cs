//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.Family;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Parser.Parsers;
using NosCore.Shared.I18N;
using SpecLight;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class FamilyUpgradeParserTests
    {
        // The family manager's list from a packet trace, and a normal shop's list for contrast.
        private const string FamilyShopTrace = "n_inv 2 2089 0 9679|2|0 9680|2|1 9681|2|2 9682|2|3 9683|2|4 9684|2|5 9685|2|6 9686|2|7 9687|2|8 9688|2|9 9689|2|10 9690|2|11 9691|2|12 9692|2|13 9693|2|14 9694|2|15 9695|2|16 9696|2|17 9697|2|18";
        private const string ItemShopTrace = "n_inv 2 2080 0 100 1.0.1084.-1.3000 1.1.1085.-1.5000";

        private string _folder;
        private List<FamilyUpgradeDto> _saved;
        private FamilyUpgradeParser _parser;
        private readonly List<string[]> _packets = new();

        [TestInitialize]
        public void Setup()
        {
            _folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_folder);
            _saved = new List<FamilyUpgradeDto>();
            var dao = new Mock<IDao<FamilyUpgradeDto, short>>();
            dao.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<FamilyUpgradeDto>>()))
                .Callback<IEnumerable<FamilyUpgradeDto>>(rows => _saved.AddRange(rows))
                .ReturnsAsync(true);
            _parser = new FamilyUpgradeParser(dao.Object, NullLoggerFactory.Instance,
                new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object);
            File.WriteAllText(Path.Combine(_folder, "Item.dat"), string.Join("\r\n#========================================================\r\n",
                Row(1084, 3000, "0\t0\t0\t0"),
                Row(9693, 3000000, "19\t9692\t3\t2"),
                Row(9694, 500000, "2\t0\t4\t0"),
                Row(9695, 2000000, "7\t9694\t4\t1"),
                Row(9696, 5000000, "5\t0\t5\t0"),
                Row(9697, 10000000, "9\t9696\t5\t1")));
        }

        [TestCleanup]
        public void Cleanup() => Directory.Delete(_folder, true);

        [TestMethod]
        public async Task TheManagersListIsReadWithEachItemsRequirements()
        {
            await new Spec("Every wired extension the family manager lists is stored with its level, line and tier")
                .Given(ThePacketTraceHolds_, FamilyShopTrace)
                .And(ThePacketTraceHolds_, ItemShopTrace)
                .WhenAsync(ParsingFamilyUpgrades)
                .Then(TheUpgradesShouldBe_, "9694:2:Warehouse:0 9695:7:Warehouse:1 9696:5:MemberLimit:0 9697:9:MemberLimit:1")
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NothingIsStoredWithoutTheManagersList()
        {
            await new Spec("A trace without the family manager's list stores no upgrade")
                .Given(ThePacketTraceHolds_, ItemShopTrace)
                .WhenAsync(ParsingFamilyUpgrades)
                .Then(TheUpgradesShouldBe_, string.Empty)
                .ExecuteAsync();
        }

        private static string Row(short vnum, long price, string data) =>
            $"\tVNUM\t{vnum}\t{price}\r\n" +
            "\tNAME\tzts1e\r\n" +
            "\tINDEX\t1\t4\t0\t0\t2505\t-1\r\n" +
            "\tTYPE\t0\t0\r\n" +
            "\tFLAG\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\r\n" +
            $"\tDATA\t{data}\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\r\n" +
            "\tBUFF\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\r\n" +
            "\tLINEDESC\t1\r\n" +
            "zts2e\r\n" +
            "\tEND";

        private void ThePacketTraceHolds_(string line) => _packets.Add(line.Split(' '));

        private Task ParsingFamilyUpgrades() => _parser.InsertFamilyUpgradesAsync(_folder, _packets);

        private void TheUpgradesShouldBe_(string expected) =>
            Assert.AreEqual(expected, string.Join(" ", _saved.OrderBy(s => s.ItemVNum)
                .Select(s => $"{s.ItemVNum}:{s.FamilyLevelMinimum}:{s.UpgradeType}:{s.UpgradeLevel}")));
    }
}
