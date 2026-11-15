//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Shops;
using NosCore.Data.StaticEntities;
using NosCore.Parser.Parsers;
using NosCore.Shared.I18N;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class ShopTypeServiceParserTests
    {
        private readonly List<ShopTypeServiceDto> _saved = new();
        private readonly string _folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        private readonly ShopTypeServiceParser _parser;

        public ShopTypeServiceParserTests()
        {
            Directory.CreateDirectory(_folder);
            var rows = new Mock<IDao<ShopTypeServiceDto, int>>();
            rows.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<ShopTypeServiceDto>>()))
                .Callback<IEnumerable<ShopTypeServiceDto>>(r => _saved.AddRange(r))
                .ReturnsAsync(true);
            _parser = new ShopTypeServiceParser(rows.Object, NullLogger<ShopTypeServiceParser>.Instance,
                new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object);
        }

        [TestCleanup]
        public void Cleanup() => Directory.Delete(_folder, true);

        private Task ParseAsync(params string[] lines)
        {
            File.WriteAllText(Path.Combine(_folder, "shoptype.dat"), string.Join("\r", lines));
            return _parser.InsertShopTypeServicesAsync(_folder);
        }

        private IEnumerable<NpcService> ServicesOf(byte shopType) =>
            _saved.Where(r => r.ShopType == shopType).Select(r => r.Service);

        [TestMethod]
        public async Task EachShopTypeGetsItsHandledServices()
        {
            await ParseAsync("#4 upgrade", "#Alchemist", "7 11 12 13 20 45 94", "#Upgrader", "40 4 6 12");

            CollectionAssert.AreEquivalent(new[] { NpcService.ResistanceSum, NpcService.SpecialistCardUpgrade,
                NpcService.SpecialistPerfection }, ServicesOf(7).ToArray());
            CollectionAssert.AreEquivalent(new[] { NpcService.Upgrade, NpcService.Rarify, NpcService.ResistanceSum },
                ServicesOf(40).ToArray());
        }

        [TestMethod]
        public async Task CommentsAndBlankLinesAreSkipped()
        {
            await ParseAsync("# 48 41", "", "#Family manager", "48 41 ");

            Assert.AreEqual(1, _saved.Count);
            Assert.AreEqual(NpcService.FamilyWarehouse, ServicesOf(48).Single());
        }

        [TestMethod]
        public async Task EveryRowHasItsOwnId()
        {
            await ParseAsync("7 12 20 45", "16 2 12");

            Assert.AreEqual(_saved.Count, _saved.Select(r => r.ShopTypeServiceId).Distinct().Count());
        }
    }
}
