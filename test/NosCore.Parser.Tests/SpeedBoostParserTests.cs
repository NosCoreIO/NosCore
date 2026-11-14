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
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class SpeedBoostParserTests
    {
        private const short Jaguar = 5196;
        private const short Unicorn = 5319;
        private const short Wings = 2000;
        private const short Sweeper = 5152;
        private const short PowerLoader = 1618;
        private const short SpeedyAlpaca = 5807;
        private const short Skateboard = 5236;
        private const short Turtlecopter = 9149;
        private const short RodeoBull = 9220;

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
            _parser = new SpeedBoostParser(items.Object, NullLogger<SpeedBoostParser>.Instance, new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object);
        }

        [TestMethod]
        public async Task AListedMountGetsItsBoost()
        {
            _items.Add(new ItemDto { VNum = Jaguar });

            await _parser.InsertSpeedBoostsAsync();

            var jaguar = _savedItems.Single();
            Assert.AreEqual(((short)3, (byte)5, (byte)4), (jaguar.SpeedBoost, jaguar.SpeedBoostDuration, jaguar.SpeedBoostRemovesDebuffsUpToLevel));
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
        public async Task AMountThatMovesItsRiderForwardsGetsItsDash()
        {
            _items.Add(new ItemDto { VNum = Sweeper });

            await _parser.InsertSpeedBoostsAsync();

            Assert.AreEqual((byte)5, _savedItems.Single().SpeedBoostDashCells);
        }

        [TestMethod]
        public async Task AMountItsBoosterSlowsDownGetsANegativeBoost()
        {
            _items.Add(new ItemDto { VNum = PowerLoader });

            await _parser.InsertSpeedBoostsAsync();

            var loader = _savedItems.Single();
            Assert.AreEqual(((short)-4, (byte)10, (byte)4), (loader.SpeedBoost, loader.SpeedBoostDuration, loader.SpeedBoostRemovesDebuffsUpToLevel));
        }

        [TestMethod]
        public async Task AMountThatRestoresHpAloneKeepsItApartFromHpAndMp()
        {
            _items.Add(new ItemDto { VNum = SpeedyAlpaca });
            _items.Add(new ItemDto { VNum = Skateboard });

            await _parser.InsertSpeedBoostsAsync();

            var alpaca = _savedItems.Single(i => i.VNum == SpeedyAlpaca);
            var skateboard = _savedItems.Single(i => i.VNum == Skateboard);
            Assert.AreEqual(((byte)10, (byte)0), (alpaca.SpeedBoostRestoresHpPerLevel, alpaca.SpeedBoostRestoresPerLevel));
            Assert.AreEqual(((byte)0, (byte)15), (skateboard.SpeedBoostRestoresHpPerLevel, skateboard.SpeedBoostRestoresPerLevel));
        }

        [TestMethod]
        public async Task AMountWithABuffWhenTheBoosterIsUsedGetsItsCard()
        {
            _items.Add(new ItemDto { VNum = RodeoBull });

            await _parser.InsertSpeedBoostsAsync();

            Assert.AreEqual(((short)853, (short)0), (_savedItems.Single().SpeedBoostStartCardId, _savedItems.Single().SpeedBoostEndCardId));
        }

        [TestMethod]
        public async Task AMountThatDodgesAndIsDizzyAfterwardsGetsBoth()
        {
            _items.Add(new ItemDto { VNum = Turtlecopter });

            await _parser.InsertSpeedBoostsAsync();

            var copter = _savedItems.Single();
            Assert.AreEqual(((byte)90, (short)786, (short)0), (copter.SpeedBoostDodgeChance, copter.SpeedBoostEndCardId, copter.SpeedBoostStartCardId));
        }

        [TestMethod]
        public void EveryMountHasABoost()
        {
            foreach (var vnum in Mounts.All.Keys)
            {
                Assert.IsTrue(MountSpeedBoosts.All.ContainsKey(vnum), $"mount {vnum}");
            }
        }

        [TestMethod]
        public void EveryBoostEitherLastsOrTeleports()
        {
            foreach (var (vnum, boost) in MountSpeedBoosts.All)
            {
                Assert.AreNotEqual(boost.TeleportsRandomly, boost.Duration > 0, $"mount {vnum}");
                Assert.AreEqual(boost.Duration > 0, boost.Speed != 0, $"mount {vnum}");
            }
        }

        [TestMethod]
        [DataRow((short)9220, (short)853)]
        [DataRow((short)9221, (short)853)]
        [DataRow((short)9276, (short)940)]
        [DataRow((short)9286, (short)940)]
        [DataRow((short)9443, (short)1439)]
        [DataRow((short)9589, (short)1439)]
        [DataRow((short)13415, (short)4223)]
        [DataRow((short)13752, (short)4329)]
        public void AMountThatTriggersABuffNamesItsCard(short vnum, short card)
        {
            Assert.AreEqual(card, MountSpeedBoosts.All[vnum].StartCard);
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
