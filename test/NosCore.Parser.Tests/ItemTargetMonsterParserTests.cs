//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Parser.Parsers;
using NosCore.Shared.I18N;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class ItemTargetMonsterParserTests
    {
        private const short SuctionFunnel = 1173;
        private const short RookieKenkoSwordsman = 145;
        private const short EliteKenkoRaider = 153;
        private const short KenkoSpy = 403;

        private readonly List<ItemDto> _items = new();
        private readonly List<NpcMonsterDto> _monsters = new();
        private readonly List<ItemTargetMonsterDto> _saved = new();
        private ItemTargetMonsterParser _parser;

        [TestInitialize]
        public void Setup()
        {
            var items = new Mock<IDao<ItemDto, short>>();
            items.Setup(s => s.LoadAll()).Returns(_items);
            var monsters = new Mock<IDao<NpcMonsterDto, short>>();
            monsters.Setup(s => s.LoadAll()).Returns(_monsters);
            var rows = new Mock<IDao<ItemTargetMonsterDto, int>>();
            rows.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<ItemTargetMonsterDto>>()))
                .Callback<IEnumerable<ItemTargetMonsterDto>>(r => _saved.AddRange(r))
                .ReturnsAsync(true);
            _parser = new ItemTargetMonsterParser(items.Object, monsters.Object, rows.Object,
                NullLogger<ItemTargetMonsterParser>.Instance, new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object);
            _monsters.AddRange(new short[] { 145, 146, 147, 148, 149, 150, 151, 152, 153, 269, 316, 403, 979 }
                .Select(v => new NpcMonsterDto { NpcMonsterVNum = v }));
        }

        [TestMethod]
        public async Task TheSuctionFunnelWorksOnTheKenkoSoldiersButNotTheSpy()
        {
            _items.Add(new ItemDto { VNum = SuctionFunnel });

            await _parser.InsertItemTargetMonstersAsync();

            var targets = _saved.Where(r => r.ItemVNum == SuctionFunnel).Select(r => r.NpcMonsterVNum).ToList();
            Assert.AreEqual(12, targets.Count);
            CollectionAssert.Contains(targets, RookieKenkoSwordsman);
            CollectionAssert.Contains(targets, EliteKenkoRaider);
            CollectionAssert.DoesNotContain(targets, KenkoSpy);
            Assert.AreEqual(_saved.Count, _saved.Select(r => r.ItemTargetMonsterId).Distinct().Count());
        }

        [TestMethod]
        public async Task ItemsAndMonstersMissingFromTheDataAreSkipped()
        {
            await _parser.InsertItemTargetMonstersAsync();
            Assert.AreEqual(0, _saved.Count);

            _items.Add(new ItemDto { VNum = SuctionFunnel });
            _monsters.RemoveAll(m => m.NpcMonsterVNum != RookieKenkoSwordsman);

            await _parser.InsertItemTargetMonstersAsync();

            Assert.AreEqual(RookieKenkoSwordsman, _saved.Single().NpcMonsterVNum);
        }
    }
}
