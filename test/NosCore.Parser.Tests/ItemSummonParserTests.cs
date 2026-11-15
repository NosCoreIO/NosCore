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
    public class ItemSummonParserTests
    {
        private const short SealedVessel = 1945;
        private const short EventSealedVessel = 1946;
        private const short SealedPinkJelly = 1386;
        private const short SealedSladeGolem = 1405;

        private readonly List<ItemDto> _items = new();
        private readonly List<NpcMonsterDto> _monsters = new();
        private readonly List<ItemSummonDto> _saved = new();
        private ItemSummonParser _parser;

        [TestInitialize]
        public void Setup()
        {
            var items = new Mock<IDao<ItemDto, short>>();
            items.Setup(s => s.LoadAll()).Returns(_items);
            var monsters = new Mock<IDao<NpcMonsterDto, short>>();
            monsters.Setup(s => s.LoadAll()).Returns(_monsters);
            var rows = new Mock<IDao<ItemSummonDto, int>>();
            rows.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<ItemSummonDto>>()))
                .Callback<IEnumerable<ItemSummonDto>>(r => _saved.AddRange(r))
                .ReturnsAsync(true);
            _parser = new ItemSummonParser(items.Object, monsters.Object, rows.Object,
                NullLogger<ItemSummonParser>.Instance, new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object);
            _monsters.AddRange(Enumerable.Range(SealedPinkJelly, 20).Select(v => new NpcMonsterDto { NpcMonsterVNum = (short)v }));
        }

        [TestMethod]
        public async Task ASealedVesselReleasesTheTwentySealedMonsters()
        {
            _items.Add(new ItemDto { VNum = SealedVessel });

            await _parser.InsertItemSummonsAsync();

            var released = _saved.Where(r => r.ItemVNum == SealedVessel).Select(r => r.NpcMonsterVNum).ToList();
            Assert.AreEqual(20, released.Count);
            CollectionAssert.Contains(released, SealedPinkJelly);
            CollectionAssert.Contains(released, SealedSladeGolem);
        }

        [TestMethod]
        public async Task EveryRowHasItsOwnId()
        {
            _items.Add(new ItemDto { VNum = SealedVessel });
            _items.Add(new ItemDto { VNum = EventSealedVessel });

            await _parser.InsertItemSummonsAsync();

            Assert.AreEqual(40, _saved.Count);
            Assert.AreEqual(_saved.Count, _saved.Select(r => r.ItemSummonId).Distinct().Count());
        }

        [TestMethod]
        public async Task ItemsAndMonstersMissingFromTheDataAreSkipped()
        {
            _items.Add(new ItemDto { VNum = SealedVessel });
            _monsters.RemoveAll(m => m.NpcMonsterVNum != SealedPinkJelly);

            await _parser.InsertItemSummonsAsync();

            Assert.AreEqual(SealedPinkJelly, _saved.Single().NpcMonsterVNum);
        }
    }
}
