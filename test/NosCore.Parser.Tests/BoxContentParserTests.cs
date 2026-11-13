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
using SpecLight;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class BoxContentParserTests
    {
        private const short Chest = 1401;
        private const short PackOfAll = 1562;
        private const short Scroll = 1218;
        private const short Pig = 448;
        private const short Food = 2187;
        private const short LimitedFood = 10030;

        private string _folder;
        private readonly List<ItemDto> _items = new();
        private readonly List<ItemDto> _savedItems = new();
        private readonly List<RollGeneratedItemDto> _rolls = new();
        private readonly List<string> _text = new();
        private readonly List<string> _rows = new();
        private BoxContentParser _parser;

        [TestInitialize]
        public void Setup()
        {
            _folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_folder);
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
            _parser = new BoxContentParser(items.Object, rolls.Object, NullLogger<BoxContentParser>.Instance);

            Item_Named_(Scroll, "Equipment Protection Scroll");
            Item_Named_(Pig, "New Year Lucky Pig");
            Item_Named_(Food, "Special Pet Food");
            Item_Named_(LimitedFood, "Special Pet Food");
        }

        [TestCleanup]
        public void Cleanup() => Directory.Delete(_folder, true);

        [TestMethod]
        public async Task AChestGivingOneOfItsListRollsEachLine()
        {
            await new Spec("A chest whose description gives one of its list becomes a box rolling each line alike")
                .Given(Chest_Describing_, Chest, "A legendary treasure is hidden inside this chest![n]Open the box to receive one of the following items:[n]1x Equipment Protection Scroll[n]3x New Year Lucky Pig[n]")
                .WhenAsync(ParsingBoxContents)
                .Then(_ShouldBeABox, Chest)
                .And(_ShouldRoll_, Chest, "1218x1 448x3")
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ANameSharedByALimitedCopyMeansTheOriginal()
        {
            await new Spec("A listed name that a limited copy shares gives the original item")
                .Given(Chest_Describing_, Chest, "You will receive one of the following items:[n]2x Special Pet Food[n]1x New Year Lucky Pig[n]")
                .WhenAsync(ParsingBoxContents)
                .Then(_ShouldRoll_, Chest, "2187x2 448x1")
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APackGivingEverythingIsLeftAlone()
        {
            await new Spec("A pack that gives its whole list is not a single roll")
                .Given(Chest_Describing_, PackOfAll, "Let's hunt the Chicken King! This package contains:[n]1x Equipment Protection Scroll[n]1x New Year Lucky Pig[n]")
                .WhenAsync(ParsingBoxContents)
                .Then(_ShouldNotBeABox, PackOfAll)
                .And(_ShouldRoll_, PackOfAll, string.Empty)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AListWithAnUnknownItemIsLeftAlone()
        {
            await new Spec("A chest listing an item no row is named after stays as it is")
                .Given(Chest_Describing_, Chest, "Open the box to receive one of the following items:[n]1x Equipment Protection Scroll[n]1x Asian Black Bear Hat[n]")
                .WhenAsync(ParsingBoxContents)
                .Then(_ShouldNotBeABox, Chest)
                .And(_ShouldRoll_, Chest, string.Empty)
                .ExecuteAsync();
        }

        private void Item_Named_(short vnum, string name) => Row(vnum, name, string.Empty);

        private void Chest_Describing_(short vnum, string description) => Row(vnum, $"Chest {vnum}", description);

        private void Row(short vnum, string name, string description)
        {
            _items.Add(new ItemDto { VNum = vnum, Effect = ItemEffectType.NoEffect });
            _text.Add($"zts{vnum}e\t{name}");
            _text.Add($"zts{vnum}d\t{description}");
            _rows.Add($"\tVNUM\t{vnum}\t0\r\n\tNAME\tzts{vnum}e\r\n\tINDEX\t1\t4\t0\t0\t{vnum}\t-1\r\n" +
                $"\tLINEDESC\t1\r\nzts{vnum}d\r\n\tEND\r\n");
        }

        private Task ParsingBoxContents()
        {
            File.WriteAllText(Path.Combine(_folder, "Item.dat"), string.Join("#====\r\n", _rows));
            File.WriteAllText(Path.Combine(_folder, "_code_uk_Item.txt"), string.Join("\r", _text));
            return _parser.InsertBoxContentsAsync(_folder);
        }

        private void _ShouldBeABox(short vnum) =>
            Assert.AreEqual(ItemEffectType.BoxEffect, _savedItems.Single(i => i.VNum == vnum).Effect);

        private void _ShouldNotBeABox(short vnum) => Assert.IsFalse(_savedItems.Any(i => i.VNum == vnum));

        private void _ShouldRoll_(short box, string rolls)
        {
            var actual = _rolls.Where(r => r.OriginalItemVNum == box)
                .Select(r => $"{r.ItemGeneratedVNum}x{r.ItemGeneratedAmount}");
            Assert.AreEqual(rolls, string.Join(' ', actual));
            Assert.IsTrue(_rolls.All(r => r.Probability == 1));
        }
    }
}
