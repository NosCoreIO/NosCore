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
    public class CostumeMorphParserTests
    {
        private const short HalloweenCostumeScroll = 1913;
        private const short BookOfHalloweenTransformation = 1914;
        private const short YakWarrior = 446;
        private const short StrongWhitewitch = 438;

        private readonly List<ItemDto> _items = new();
        private readonly List<NpcMonsterDto> _monsters = new();
        private readonly List<CostumeMorphDto> _saved = new();
        private CostumeMorphParser _parser;

        [TestInitialize]
        public void Setup()
        {
            var items = new Mock<IDao<ItemDto, short>>();
            items.Setup(s => s.LoadAll()).Returns(_items);
            var monsters = new Mock<IDao<NpcMonsterDto, short>>();
            monsters.Setup(s => s.LoadAll()).Returns(_monsters);
            var rows = new Mock<IDao<CostumeMorphDto, int>>();
            rows.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<CostumeMorphDto>>()))
                .Callback<IEnumerable<CostumeMorphDto>>(r => _saved.AddRange(r))
                .ReturnsAsync(true);
            _parser = new CostumeMorphParser(items.Object, monsters.Object, rows.Object,
                NullLogger<CostumeMorphParser>.Instance, new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object);
            _monsters.AddRange(CostumeScrolls.All.Values.SelectMany(v => v).Distinct()
                .Select(v => new NpcMonsterDto { NpcMonsterVNum = v }));
        }

        [TestMethod]
        public async Task TheHalloweenCostumeScrollGivesItsTenMonsterLooks()
        {
            _items.Add(new ItemDto { VNum = HalloweenCostumeScroll });

            await _parser.InsertCostumeMorphsAsync();

            var looks = _saved.Where(r => r.ItemVNum == HalloweenCostumeScroll).Select(r => r.Morph).ToList();
            Assert.AreEqual(10, looks.Count);
            CollectionAssert.Contains(looks, (short)(1000 + YakWarrior));
            CollectionAssert.Contains(looks, (short)(1000 + StrongWhitewitch));
        }

        [TestMethod]
        public async Task TheBookOfHalloweenTransformationGivesTheFirstFive()
        {
            _items.Add(new ItemDto { VNum = BookOfHalloweenTransformation });

            await _parser.InsertCostumeMorphsAsync();

            Assert.AreEqual(5, _saved.Count);
            CollectionAssert.DoesNotContain(_saved.Select(r => r.Morph).ToList(), (short)(1000 + StrongWhitewitch));
        }

        [TestMethod]
        public async Task EveryRowHasItsOwnId()
        {
            _items.Add(new ItemDto { VNum = HalloweenCostumeScroll });
            _items.Add(new ItemDto { VNum = BookOfHalloweenTransformation });

            await _parser.InsertCostumeMorphsAsync();

            Assert.AreEqual(15, _saved.Count);
            Assert.AreEqual(_saved.Count, _saved.Select(r => r.CostumeMorphId).Distinct().Count());
        }

        [TestMethod]
        public async Task ItemsAndMonstersMissingFromTheDataAreSkipped()
        {
            _items.Add(new ItemDto { VNum = HalloweenCostumeScroll });
            _monsters.RemoveAll(m => m.NpcMonsterVNum != YakWarrior);

            await _parser.InsertCostumeMorphsAsync();

            Assert.AreEqual((short)(1000 + YakWarrior), _saved.Single().Morph);
        }
    }
}
