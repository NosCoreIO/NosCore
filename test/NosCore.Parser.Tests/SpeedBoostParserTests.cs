//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
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
    public class SpeedBoostParserTests
    {
        private const short Jaguar = 5834;
        private const short Skateboard = 5236;
        private const short Unicorn = 5319;
        private const short Wings = 2000;

        private string _folder;
        private readonly List<ItemDto> _items = new();
        private readonly List<ItemDto> _savedItems = new();
        private readonly List<string> _text = new();
        private readonly List<string> _rows = new();
        private SpeedBoostParser _parser;

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
            _parser = new SpeedBoostParser(items.Object, NullLogger<SpeedBoostParser>.Instance);
        }

        [TestCleanup]
        public void Cleanup() => Directory.Delete(_folder, true);

        [TestMethod]
        public async Task SpeedDurationAndDebuffRemovalComeFromTheDescription()
        {
            await new Spec("A mount's booster lines set its speed, duration and the debuffs it removes")
                .Given(Mount_Describing_, Jaguar, "A fast jaguar.[n][With Speed Booster][n]Removes all negative effects under level 4.[n]Movement speed +4[n]Duration +2[n][Note][n]Speed +9 in races.[n]")
                .WhenAsync(ParsingSpeedBoosts)
                .Then(_ShouldBoost_, Jaguar, "speed 4, 2 s, restores 0, debuffs 4, random False")
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARestoringMountHealsByLevel()
        {
            await new Spec("Restoring HP and MP by level is read with its factor")
                .Given(Mount_Describing_, Skateboard, "[With Speed Booster][n]Restores HP and MP (your level *15).[n]Speed +3[n]Duration +5[n]")
                .WhenAsync(ParsingSpeedBoosts)
                .Then(_ShouldBoost_, Skateboard, "speed 3, 5 s, restores 15, debuffs 0, random False")
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARandomTeleportNeedsNoSpeed()
        {
            await new Spec("A mount that only teleports randomly is still boosted")
                .Given(Mount_Describing_, Unicorn, "[With Speed Booster][n]Teleports you to a random point on the map.[n]")
                .WhenAsync(ParsingSpeedBoosts)
                .Then(_ShouldBoost_, Unicorn, "speed 0, 0 s, restores 0, debuffs 0, random True")
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnItemWithoutTheHeadingIsLeftAlone()
        {
            await new Spec("An item whose description has no booster heading is not touched")
                .Given(Mount_Describing_, Wings, "Movement speed +2[n]Duration +3[n]")
                .WhenAsync(ParsingSpeedBoosts)
                .Then(_ShouldNotBeSaved, Wings)
                .ExecuteAsync();
        }

        private void Mount_Describing_(short vnum, string description)
        {
            _items.Add(new ItemDto { VNum = vnum });
            _text.Add($"zts{vnum}e\tMount {vnum}");
            _text.Add($"zts{vnum}d\t{description}");
            _rows.Add($"\tVNUM\t{vnum}\t0\r\n\tNAME\tzts{vnum}e\r\n\tINDEX\t0\t0\t0\t0\t{vnum}\t-1\r\n" +
                $"\tLINEDESC\t1\r\nzts{vnum}d\r\n\tEND\r\n");
        }

        private Task ParsingSpeedBoosts()
        {
            File.WriteAllText(Path.Combine(_folder, "Item.dat"), string.Join("#====\r\n", _rows));
            File.WriteAllText(Path.Combine(_folder, "_code_uk_Item.txt"), string.Join("\r", _text));
            return _parser.InsertSpeedBoostsAsync(_folder);
        }

        private void _ShouldBoost_(short vnum, string boost)
        {
            var item = _savedItems.Single(i => i.VNum == vnum);
            Assert.AreEqual(boost, $"speed {item.SpeedBoost}, {item.SpeedBoostDuration} s, restores {item.SpeedBoostRestoresPerLevel}, " +
                $"debuffs {item.SpeedBoostRemovesDebuffsUpToLevel}, random {item.SpeedBoostTeleportsRandomly}");
        }

        private void _ShouldNotBeSaved(short vnum) => Assert.IsFalse(_savedItems.Any(i => i.VNum == vnum));
    }
}
