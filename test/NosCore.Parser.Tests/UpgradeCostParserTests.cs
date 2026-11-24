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
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Packets.Enumerations;
using NosCore.Parser.Parsers;
using NosCore.Shared.I18N;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class UpgradeCostParserTests
    {
        private readonly List<ItemDto> _items = new();
        private readonly List<UpgradeCostDto> _costs = new();
        private readonly List<UpgradeCostMaterialDto> _materials = new();
        private UpgradeCostParser _parser;

        [TestInitialize]
        public void Setup()
        {
            var items = new Mock<IDao<ItemDto, short>>();
            items.Setup(s => s.LoadAll()).Returns(_items);
            var costs = new Mock<IDao<UpgradeCostDto, short>>();
            costs.Setup(s => s.LoadAll()).Returns(() => _costs.ToList());
            costs.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<UpgradeCostDto>>()))
                .Callback<IEnumerable<UpgradeCostDto>>(rows => Upsert(_costs, rows, r => r.UpgradeCostId))
                .ReturnsAsync(true);
            var materials = new Mock<IDao<UpgradeCostMaterialDto, short>>();
            materials.Setup(s => s.LoadAll()).Returns(() => _materials.ToList());
            materials.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<UpgradeCostMaterialDto>>()))
                .Callback<IEnumerable<UpgradeCostMaterialDto>>(rows => Upsert(_materials, rows, r => r.UpgradeCostMaterialId))
                .ReturnsAsync(true);
            _parser = new UpgradeCostParser(items.Object, costs.Object, materials.Object,
                NullLogger<UpgradeCostParser>.Instance, new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object);
        }

        [TestMethod]
        public async Task ATattooTakesGoldAndFiveMaterials()
        {
            AllItemsExist();

            await _parser.InsertUpgradeCostsAsync();

            var tattoo = _costs.Single(s => s.Kind == UpgradePacketType.ApplyTattoo);
            Assert.AreEqual(20_000L, tattoo.Gold);
            CollectionAssert.AreEquivalent(new[] { (2411, 15), (2416, 20), (2408, 20), (2460, 15), (2406, 10) },
                _materials.Where(s => s.UpgradeCostId == tattoo.UpgradeCostId).Select(s => ((int)s.ItemVNum, (int)s.Amount)).ToList());
        }

        [TestMethod]
        public async Task ImportingTwiceKeepsOneRowPerCost()
        {
            AllItemsExist();

            await _parser.InsertUpgradeCostsAsync();
            await _parser.InsertUpgradeCostsAsync();

            Assert.AreEqual(UpgradeCosts.All.Count, _costs.Count);
            Assert.AreEqual(UpgradeCosts.All.Sum(s => s.Materials.Length), _materials.Count);
        }

        [TestMethod]
        public async Task ACostWithAnUnknownMaterialIsLeftOut()
        {
            AllItemsExist();
            _items.RemoveAll(s => s.VNum == 2406);

            await _parser.InsertUpgradeCostsAsync();

            Assert.IsFalse(_costs.Any(s => s.Kind == UpgradePacketType.ApplyTattoo));
        }

        private void AllItemsExist() =>
            _items.AddRange(UpgradeCosts.All.SelectMany(s => s.Materials.Select(m => m.ItemVNum))
                .Distinct().Select(v => new ItemDto { VNum = v }));

        private static void Upsert<T>(List<T> stored, IEnumerable<T> rows, System.Func<T, short> key)
        {
            foreach (var row in rows)
            {
                stored.RemoveAll(s => key(s) == key(row));
                stored.Add(row);
            }
        }
    }
}
