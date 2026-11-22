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
using NosCore.Data.StaticEntities;
using NosCore.Parser.Parsers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Shared.I18N;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class RefinerParserTests
    {
        private const short SoulGemRefiner = 1036;
        private const short CellonRefiner = 1037;

        private readonly List<ItemDto> _items = new();
        private readonly List<RollGeneratedItemDto> _existingRolls = new();
        private readonly List<RollGeneratedItemDto> _savedRolls = new();
        private RefinerParser _parser;

        [TestInitialize]
        public void Setup()
        {
            var items = new Mock<IDao<ItemDto, short>>();
            items.Setup(s => s.LoadAll()).Returns(_items);
            var rolls = new Mock<IDao<RollGeneratedItemDto, short>>();
            rolls.Setup(s => s.LoadAll()).Returns(_existingRolls);
            rolls.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<RollGeneratedItemDto>>()))
                .Callback<IEnumerable<RollGeneratedItemDto>>(rows => _savedRolls.AddRange(rows))
                .ReturnsAsync(true);
            _parser = new RefinerParser(items.Object, rolls.Object, NullLogger<RefinerParser>.Instance, new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object);
            _items.AddRange(Enumerable.Range(1014, 13).Select(v => new ItemDto { VNum = (short)v }));
        }

        [TestMethod]
        public async Task ASoulGemRefinerDrawsCellaThenAGemOrNothing()
        {
            _items.Add(Refiner(SoulGemRefiner, RefinerType.SoulGem));

            await _parser.InsertRefinerRollsAsync();

            var cella = _savedRolls.Where(r => r.RollGroup == RefinerParser.CellaRoll).ToList();
            CollectionAssert.AreEqual(new short[] { 5, 6, 7, 8, 9, 10 }, cella.Select(r => r.ItemGeneratedAmount).ToList());
            Assert.IsTrue(cella.All(r => r.ItemGeneratedVNum == Refiners.CellaPowder && r.Probability == 1));
            var extras = _savedRolls.Where(r => r.RollGroup == RefinerParser.ExtraRoll).ToList();
            CollectionAssert.AreEquivalent(new short[] { 1015, 1016, Refiners.CellaPowder }, extras.Select(r => r.ItemGeneratedVNum).ToList());
            Assert.AreEqual((int)Refiners.ChanceScale, extras.Sum(r => (int)r.Probability));
            Assert.AreEqual((short)0, extras.Single(r => r.ItemGeneratedVNum == Refiners.CellaPowder).ItemGeneratedAmount);
        }

        [TestMethod]
        public async Task EachKindOfRefinerGetsItsOwnExtras()
        {
            _items.Add(Refiner(CellonRefiner, RefinerType.Cellon));

            await _parser.InsertRefinerRollsAsync();

            CollectionAssert.AreEquivalent(new short[] { 1017, 1018, 1019, 1020, 1021, 1022 },
                _savedRolls.Where(r => r.RollGroup == RefinerParser.ExtraRoll && r.ItemGeneratedAmount > 0)
                    .Select(r => r.ItemGeneratedVNum).ToList());
            Assert.IsTrue(_savedRolls.All(r => r.OriginalItemVNum == CellonRefiner));
        }

        [TestMethod]
        public async Task TheRollsAreNumberedAfterTheRowsAlreadyThere()
        {
            _existingRolls.Add(new RollGeneratedItemDto { RollGeneratedItemId = 40 });
            _items.Add(Refiner(SoulGemRefiner, RefinerType.SoulGem));

            await _parser.InsertRefinerRollsAsync();

            Assert.AreEqual((short)41, _savedRolls.Min(r => r.RollGeneratedItemId));
            Assert.AreEqual(_savedRolls.Count, _savedRolls.Select(r => r.RollGeneratedItemId).Distinct().Count());
        }

        [TestMethod]
        public async Task ARefinerWhoseItemsAreMissingIsLeftAlone()
        {
            _items.RemoveAll(i => i.VNum == 1016);
            _items.Add(Refiner(SoulGemRefiner, RefinerType.SoulGem));

            await _parser.InsertRefinerRollsAsync();

            Assert.AreEqual(0, _savedRolls.Count);
        }

        private static ItemDto Refiner(short vnum, RefinerType kind) => new()
        {
            VNum = vnum,
            ItemType = ItemType.Main,
            Effect = ItemEffectType.Refiner,
            EffectValue = (int)kind,
        };
    }
}
