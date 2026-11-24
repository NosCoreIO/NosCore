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
using NosCore.Packets.Enumerations;
using NosCore.Parser.Parsers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class HairStyleDyeParserTests
    {
        private const short SavageDyes = 2533;

        private readonly List<ItemDto> _items = new();
        private readonly List<HairStyleDyeDto> _saved = new();
        private HairStyleDyeParser _parser;

        [TestInitialize]
        public void Setup()
        {
            var items = new Mock<IDao<ItemDto, short>>();
            items.Setup(s => s.LoadAll()).Returns(_items);
            var rows = new Mock<IDao<HairStyleDyeDto, int>>();
            rows.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<HairStyleDyeDto>>()))
                .Callback<IEnumerable<HairStyleDyeDto>>(r => _saved.AddRange(r))
                .ReturnsAsync(true);
            _parser = new HairStyleDyeParser(items.Object, rows.Object, NullLogger<HairStyleDyeParser>.Instance,
                new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object);
        }

        [TestMethod]
        public async Task TheSavageDyesDyeTheFauxHawkUndercutAndTheChoppyBangs()
        {
            _items.Add(new ItemDto { VNum = SavageDyes });

            await _parser.InsertHairStyleDyesAsync();

            CollectionAssert.AreEquivalent(
                new[]
                {
                    (HairStyleType.FauxHawkUndercut, HairStyleType.SavageFauxHawkUndercut),
                    (HairStyleType.ChoppyBangs, HairStyleType.SavageChoppyBangs),
                },
                _saved.Where(r => r.ItemVNum == SavageDyes).Select(r => (r.HairStyle, r.DyedHairStyle)).ToArray());
        }

        [TestMethod]
        public async Task AMissingItemGetsNoRows()
        {
            await _parser.InsertHairStyleDyesAsync();

            Assert.AreEqual(0, _saved.Count);
        }

        [TestMethod]
        public async Task RunningTwiceGivesTheSameIds()
        {
            _items.Add(new ItemDto { VNum = SavageDyes });

            await _parser.InsertHairStyleDyesAsync();
            var first = _saved.Select(r => r.HairStyleDyeId).ToList();
            _saved.Clear();
            await _parser.InsertHairStyleDyesAsync();

            CollectionAssert.AreEqual(first, _saved.Select(r => r.HairStyleDyeId).ToList());
        }
    }
}
