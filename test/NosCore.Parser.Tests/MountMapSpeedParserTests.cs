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
    public class MountMapSpeedParserTests
    {
        private const short Jaguar = 5834;
        private const short Bike = 5232;
        private const short Wings = 2000;
        private const short NosVille = 1;
        private const short MortazDesertPort = 170;
        private const short KertosJaws = 205;
        private const short Citadel = 130;

        private readonly List<ItemDto> _items = new();
        private readonly List<MountMapSpeedDto> _saved = new();
        private MountMapSpeedParser _parser;

        [TestInitialize]
        public void Setup()
        {
            var items = new Mock<IDao<ItemDto, short>>();
            items.Setup(s => s.LoadAll()).Returns(_items);
            var rows = new Mock<IDao<MountMapSpeedDto, int>>();
            rows.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<MountMapSpeedDto>>()))
                .Callback<IEnumerable<MountMapSpeedDto>>(r => _saved.AddRange(r))
                .ReturnsAsync(true);
            _parser = new MountMapSpeedParser(items.Object, rows.Object, NullLogger<MountMapSpeedParser>.Instance,
                new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object);
        }

        private byte? SpeedOn(short mount, short map) =>
            _saved.SingleOrDefault(r => r.ItemVNum == mount && r.MapId == map)?.Speed;

        [TestMethod]
        public async Task AMountGetsItsBonusOnEveryMapOfItsRegions()
        {
            _items.Add(new ItemDto { VNum = Jaguar });

            await _parser.InsertMountMapSpeedsAsync();

            Assert.AreEqual((byte)2, SpeedOn(Jaguar, MortazDesertPort));
            Assert.AreEqual((byte)2, SpeedOn(Jaguar, KertosJaws));
            Assert.AreEqual((byte)3, SpeedOn(Jaguar, Citadel));
            Assert.IsNull(SpeedOn(Jaguar, NosVille));
        }

        [TestMethod]
        public async Task EveryRowHasItsOwnId()
        {
            _items.Add(new ItemDto { VNum = Jaguar });
            _items.Add(new ItemDto { VNum = Bike });

            await _parser.InsertMountMapSpeedsAsync();

            Assert.AreEqual(_saved.Count, _saved.Select(r => r.MountMapSpeedId).Distinct().Count());
            Assert.AreEqual(_saved.Count, _saved.Select(r => (r.ItemVNum, r.MapId)).Distinct().Count());
        }

        [TestMethod]
        public async Task AMountMissingFromTheItemsIsSkipped()
        {
            _items.Add(new ItemDto { VNum = Wings });

            await _parser.InsertMountMapSpeedsAsync();

            Assert.AreEqual(0, _saved.Count);
        }

        [TestMethod]
        public void EveryListedMountIsAMount()
        {
            foreach (var vnum in MountMapSpeeds.All.Keys)
            {
                Assert.IsTrue(Mounts.All.ContainsKey(vnum), $"mount {vnum}");
            }
        }
    }
}
