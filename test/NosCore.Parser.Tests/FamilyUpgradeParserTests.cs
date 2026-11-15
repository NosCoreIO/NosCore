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
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Shop;
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
        private static readonly IPacket FamilyShopTrace = Captured.FamilyShop(2089,
            9679, 9680, 9681, 9682, 9683, 9684, 9685, 9686, 9687, 9688, 9689, 9690, 9691, 9692, 9693, 9694, 9695, 9696, 9697);

        private static readonly IPacket ItemShopTrace = Captured.ItemShop(2080,
            new NInvItemSubPacket { Type = PocketType.Main, Slot = 0, VNum = 1084, Price = 3000 },
            new NInvItemSubPacket { Type = PocketType.Main, Slot = 1, VNum = 1085, Price = 5000 });

        private string _folder;
        private List<FamilyUpgradeDto> _saved;
        private FamilyUpgradeParser _parser;
        private readonly List<IPacket> _packets = new();

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

        private void ThePacketTraceHolds_(IPacket packet) => _packets.Add(packet);

        private Task ParsingFamilyUpgrades() => _parser.InsertFamilyUpgradesAsync(_folder, _packets);

        private void TheUpgradesShouldBe_(string expected) =>
            Assert.AreEqual(expected, string.Join(" ", _saved.OrderBy(s => s.ItemVNum)
                .Select(s => $"{s.ItemVNum}:{s.FamilyLevelMinimum}:{s.UpgradeType}:{s.UpgradeLevel}")));
    }
}
