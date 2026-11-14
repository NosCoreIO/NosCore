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
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.Parser.Parsers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class BoxContentParserTests
    {
        private const short Chest = 1401;
        private const short Pack = 1564;

        private readonly List<ItemDto> _items = new();
        private readonly List<ItemDto> _savedItems = new();
        private readonly List<RollGeneratedItemDto> _rolls = new();
        private BoxContentParser _parser;

        [TestInitialize]
        public void Setup()
        {
            var items = new Mock<IDao<ItemDto, short>>();
            items.Setup(s => s.LoadAll()).Returns(_items);
            items.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<ItemDto>>()))
                .Callback<IEnumerable<ItemDto>>(rows => _savedItems.AddRange(rows))
                .ReturnsAsync(true);
            var rolls = new Mock<IDao<RollGeneratedItemDto, short>>();
            rolls.Setup(s => s.LoadAll()).Returns(new List<RollGeneratedItemDto>());
            rolls.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<RollGeneratedItemDto>>()))
                .Callback<IEnumerable<RollGeneratedItemDto>>(rows => _rolls.AddRange(rows))
                .ReturnsAsync(true);
            _parser = new BoxContentParser(items.Object, rolls.Object, NullLogger<BoxContentParser>.Instance, new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object);
        }

        [TestMethod]
        public async Task AChestGivingOneOfItsListRollsEachLineAlike()
        {
            Items(Chest, 1218, 447, 448, 2187, 1219, 287);

            await _parser.InsertBoxContentsAsync();

            Assert.AreEqual(ItemEffectType.BoxEffect, _savedItems.Single().Effect);
            CollectionAssert.AreEqual(new short[] { 1218, 447, 448, 2187, 1219, 287 }, _rolls.Select(r => r.ItemGeneratedVNum).ToArray());
            Assert.IsTrue(_rolls.All(r => r.Probability == 1 && r.OriginalItemVNum == Chest));
        }

        [TestMethod]
        public async Task APackGivesEveryLineWithItsAmount()
        {
            Items(Pack, 1130, 1131, 1122);

            await _parser.InsertBoxContentsAsync();

            Assert.AreEqual(ItemEffectType.ItemPack, _savedItems.Single().Effect);
            CollectionAssert.AreEqual(new byte[] { 1, 1, 70 }, _rolls.Select(r => r.ItemGeneratedAmount).ToArray());
        }

        [TestMethod]
        public async Task ABoxListingAMissingItemIsLeftAlone()
        {
            Items(Chest, 1218, 447);

            await _parser.InsertBoxContentsAsync();

            Assert.AreEqual(0, _savedItems.Count);
            Assert.AreEqual(0, _rolls.Count);
        }

        [TestMethod]
        public async Task ARaidBoxRollsAtTheRaidDesignAndHandsItsRarityToEquipment()
        {
            const short hatusBox = 185;
            _items.Add(new ItemDto { VNum = hatusBox, ItemType = ItemType.Box });
            _items.AddRange(new short[] { 565, 568, 571, 574, 577, 580, 583, 586, 1430, 1429, 2516, 2439, 9385 }
                .Select(v => new ItemDto { VNum = v, ItemType = ItemType.Shell }));
            _items.AddRange(new short[] { 199, 940, 941, 4979 }.Select(v => new ItemDto { VNum = v, ItemType = ItemType.Weapon }));

            await _parser.InsertBoxContentsAsync();

            Assert.AreEqual(ItemEffectType.BoxEffect, _savedItems.Single().Effect);
            Assert.AreEqual(17, _rolls.Count);
            Assert.IsTrue(_rolls.All(r => r.OriginalItemDesign == RaidBoxes.Design && r.MinimumOriginalItemRare == -2
                && r.MaximumOriginalItemRare == 8 && r.Probability == 1));
            CollectionAssert.AreEquivalent(new short[] { 199, 940, 941, 4979 },
                _rolls.Where(r => r.IsRareRandom).Select(r => r.ItemGeneratedVNum).ToArray());
            Assert.AreEqual(5, _rolls.Single(r => r.ItemGeneratedVNum == 1429).ItemGeneratedAmount);
        }

        private void Items(params short[] vnums) => _items.AddRange(vnums.Select(v => new ItemDto { VNum = v }));
    }
}
