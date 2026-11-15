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
    public class NpcExchangeParserTests
    {
        private readonly List<ItemDto> _items = new();
        private readonly List<NpcExchangeDto> _exchanges = new();
        private readonly List<NpcExchangeMaterialDto> _materials = new();
        private NpcExchangeParser _parser;

        [TestInitialize]
        public void Setup()
        {
            var items = new Mock<IDao<ItemDto, short>>();
            items.Setup(s => s.LoadAll()).Returns(_items);
            var exchanges = new Mock<IDao<NpcExchangeDto, short>>();
            exchanges.Setup(s => s.LoadAll()).Returns(() => _exchanges.ToList());
            exchanges.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<NpcExchangeDto>>()))
                .Callback<IEnumerable<NpcExchangeDto>>(rows => Upsert(_exchanges, rows, r => r.NpcExchangeId))
                .ReturnsAsync(true);
            var materials = new Mock<IDao<NpcExchangeMaterialDto, short>>();
            materials.Setup(s => s.LoadAll()).Returns(() => _materials.ToList());
            materials.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<NpcExchangeMaterialDto>>()))
                .Callback<IEnumerable<NpcExchangeMaterialDto>>(rows => Upsert(_materials, rows, r => r.NpcExchangeMaterialId))
                .ReturnsAsync(true);
            _parser = new NpcExchangeParser(items.Object, exchanges.Object, materials.Object,
                NullLogger<NpcExchangeParser>.Instance, new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object);
        }

        [TestMethod]
        public async Task TheLordDracoSealTakesItsThreeMaterials()
        {
            AllItemsExist();

            await _parser.InsertNpcExchangesAsync();

            var seal = _exchanges.Single(s => s.Runner == NrunRunnerType.ExchangeForLordDracosRaidSeal);
            Assert.AreEqual(((short)5500, (short)1), (seal.ItemVNum, seal.Amount));
            CollectionAssert.AreEquivalent(new[] { (1012, 20), (1027, 20), (1013, 20) },
                _materials.Where(s => s.NpcExchangeId == seal.NpcExchangeId).Select(s => ((int)s.ItemVNum, (int)s.Amount)).ToList());
        }

        [TestMethod]
        public async Task EachClassHasItsOwnCardForTheClaws()
        {
            AllItemsExist();

            await _parser.InsertNpcExchangesAsync();

            CollectionAssert.AreEquivalent(new short[] { 4500, 4501, 4502 },
                _exchanges.Where(s => s.Runner == NrunRunnerType.ExchangeForSP5).Select(s => s.ItemVNum).ToList());
        }

        [TestMethod]
        public async Task ImportingTwiceKeepsOneRowPerExchange()
        {
            AllItemsExist();

            await _parser.InsertNpcExchangesAsync();
            await _parser.InsertNpcExchangesAsync();

            Assert.AreEqual(NpcExchanges.All.Count, _exchanges.Count);
            Assert.AreEqual(NpcExchanges.All.Sum(s => s.Materials.Length), _materials.Count);
        }

        [TestMethod]
        public async Task AnExchangeWithAnUnknownItemIsLeftOut()
        {
            AllItemsExist();
            _items.RemoveAll(s => s.VNum == 5917);

            await _parser.InsertNpcExchangesAsync();

            Assert.IsFalse(_exchanges.Any(s => s.Runner == NrunRunnerType.ExchangeForGrenigasRaidSeal));
        }

        private void AllItemsExist() =>
            _items.AddRange(NpcExchanges.All.SelectMany(s => s.Materials.Select(m => m.ItemVNum).Append(s.ItemVNum))
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
