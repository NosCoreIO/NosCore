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
using NosCore.Shared.I18N;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class MountScrollParserTests
    {
        private const short NoviceScroll = 5833;
        private const short CamelScroll = 5923;
        private const short Camel = 5914;

        private readonly List<ItemDto> _items = new();
        private readonly List<ItemDto> _savedItems = new();
        private readonly List<RollGeneratedItemDto> _existingRolls = new();
        private readonly List<RollGeneratedItemDto> _savedRolls = new();
        private MountScrollParser _parser;

        [TestInitialize]
        public void Setup()
        {
            var items = new Mock<IDao<ItemDto, short>>();
            items.Setup(s => s.LoadAll()).Returns(_items);
            items.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<ItemDto>>()))
                .Callback<IEnumerable<ItemDto>>(rows => _savedItems.AddRange(rows))
                .ReturnsAsync(true);
            var rolls = new Mock<IDao<RollGeneratedItemDto, short>>();
            rolls.Setup(s => s.LoadAll()).Returns(_existingRolls);
            rolls.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<RollGeneratedItemDto>>()))
                .Callback<IEnumerable<RollGeneratedItemDto>>(rows => _savedRolls.AddRange(rows))
                .ReturnsAsync(true);
            _parser = new MountScrollParser(items.Object, rolls.Object, NullLogger<MountScrollParser>.Instance, new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object);
        }

        [TestMethod]
        public async Task TheNoviceScrollDrawsFromItsSixMountsAtItsOwnSpeed()
        {
            _items.Add(new ItemDto { VNum = NoviceScroll });
            _items.AddRange(new short[] { 1906, 1907, 1965, 5238, 5240, 5232 }.Select(v => new ItemDto { VNum = v }));

            await _parser.InsertMountScrollsAsync();

            var scroll = _savedItems.Single();
            Assert.AreEqual(((byte)17, (byte)69, false), (scroll.Speed, scroll.LevelMaximum, scroll.ConsumedOnMount));
            CollectionAssert.AreEquivalent(new short[] { 1906, 1907, 1965, 5238, 5240, 5232 },
                _savedRolls.Select(r => r.ItemGeneratedVNum).ToList());
        }

        [TestMethod]
        public async Task TheCamelScrollIsUsedUpAndRidesAtTheCamelsSpeed()
        {
            _items.Add(new ItemDto { VNum = CamelScroll });
            _items.Add(new ItemDto { VNum = Camel });

            await _parser.InsertMountScrollsAsync();

            var scroll = _savedItems.Single();
            Assert.AreEqual(((byte)0, true), (scroll.Speed, scroll.ConsumedOnMount));
            Assert.AreEqual(Camel, _savedRolls.Single().ItemGeneratedVNum);
        }

        [TestMethod]
        public async Task TheRollsAreNumberedAfterTheBoxContents()
        {
            _existingRolls.Add(new RollGeneratedItemDto { RollGeneratedItemId = 40 });
            _items.Add(new ItemDto { VNum = CamelScroll });
            _items.Add(new ItemDto { VNum = Camel });

            await _parser.InsertMountScrollsAsync();

            Assert.AreEqual((short)41, _savedRolls.Single().RollGeneratedItemId);
        }

        [TestMethod]
        public async Task AScrollWhoseMountIsMissingIsLeftAlone()
        {
            _items.Add(new ItemDto { VNum = CamelScroll });

            await _parser.InsertMountScrollsAsync();

            Assert.AreEqual(0, _savedItems.Count);
            Assert.AreEqual(0, _savedRolls.Count);
        }
    }
}
