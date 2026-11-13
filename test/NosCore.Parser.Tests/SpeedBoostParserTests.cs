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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class SpeedBoostParserTests
    {
        private const short Jaguar = 5196;
        private const short Unicorn = 5319;
        private const short Wings = 2000;

        private readonly List<ItemDto> _items = new();
        private readonly List<ItemDto> _savedItems = new();
        private SpeedBoostParser _parser;

        [TestInitialize]
        public void Setup()
        {
            var items = new Mock<IDao<ItemDto, short>>();
            items.Setup(s => s.LoadAll()).Returns(_items);
            items.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<ItemDto>>()))
                .Callback<IEnumerable<ItemDto>>(rows => _savedItems.AddRange(rows))
                .ReturnsAsync(true);
            _parser = new SpeedBoostParser(items.Object, NullLogger<SpeedBoostParser>.Instance);
        }

        [TestMethod]
        public async Task AListedMountGetsItsBoost()
        {
            _items.Add(new ItemDto { VNum = Jaguar });

            await _parser.InsertSpeedBoostsAsync();

            var jaguar = _savedItems.Single();
            Assert.AreEqual(((byte)3, (byte)5, (byte)4), (jaguar.SpeedBoost, jaguar.SpeedBoostDuration, jaguar.SpeedBoostRemovesDebuffsUpToLevel));
            Assert.IsFalse(jaguar.SpeedBoostTeleportsRandomly);
        }

        [TestMethod]
        public async Task ATeleportingMountGetsTheTeleportEffect()
        {
            _items.Add(new ItemDto { VNum = Unicorn });

            await _parser.InsertSpeedBoostsAsync();

            Assert.IsTrue(_savedItems.Single().SpeedBoostTeleportsRandomly);
            Assert.AreEqual(3625, _savedItems.Single().SpeedBoostTeleportEffect);
        }

        [TestMethod]
        public async Task AnItemNotListedIsLeftAlone()
        {
            _items.Add(new ItemDto { VNum = Wings });

            await _parser.InsertSpeedBoostsAsync();

            Assert.AreEqual(0, _savedItems.Count);
        }
    }
}
