//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Packets.Enumerations;
using NosCore.Parser.Parsers;
using NosCore.Shared.I18N;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class PartnerSpecialistNameParserTests
    {
        private const short FoxyCard = 4807;
        private const short Sword = 1;

        private readonly List<ItemDto> _items = new();
        private readonly List<ItemDto> _savedItems = new();
        private PartnerSpecialistNameParser _parser;

        [TestInitialize]
        public void Setup()
        {
            var items = new Mock<IDao<ItemDto, short>>();
            items.Setup(s => s.LoadAll()).Returns(_items);
            items.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<ItemDto>>()))
                .Callback<IEnumerable<ItemDto>>(rows => _savedItems.AddRange(rows))
                .ReturnsAsync(true);
            _parser = new PartnerSpecialistNameParser(items.Object, NullLogger<PartnerSpecialistNameParser>.Instance, new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object);
        }

        [TestMethod]
        public async Task APartnerCardGetsItsName()
        {
            _items.Add(new ItemDto { VNum = FoxyCard });

            await _parser.InsertPartnerSpecialistNamesAsync();

            Assert.AreEqual(Game18NConstString.Foxy, _savedItems.Single().PartnerSpecialistName);
        }

        [TestMethod]
        public async Task OtherItemsAreLeftAlone()
        {
            _items.Add(new ItemDto { VNum = Sword });

            await _parser.InsertPartnerSpecialistNamesAsync();

            Assert.AreEqual(0, _savedItems.Count);
        }
    }
}
