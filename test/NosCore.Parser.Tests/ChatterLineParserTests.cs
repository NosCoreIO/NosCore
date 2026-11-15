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
    public class ChatterLineParserTests
    {
        [TestMethod]
        public async Task EveryGroupLineIsSavedOnceWithItsGroup()
        {
            var saved = new List<ChatterLineDto>();
            var dao = new Mock<IDao<ChatterLineDto, short>>();
            dao.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<ChatterLineDto>>()))
                .Callback<IEnumerable<ChatterLineDto>>(rows => saved.AddRange(rows))
                .ReturnsAsync(true);

            await new ChatterLineParser(dao.Object, NullLogger<ChatterLineParser>.Instance,
                new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object).InsertChatterLinesAsync();

            Assert.AreEqual(ChatterLineParser.Lines.Sum(group => group.Value.Length), saved.Count);
            Assert.AreEqual(saved.Count, saved.Select(line => line.ChatterLineId).Distinct().Count());
            CollectionAssert.AreEquivalent(
                new[] { (short)Game18NConstString.Huh, (short)Game18NConstString.Uurk },
                saved.Where(line => line.ChatterGroup == 2).Select(line => line.Message).ToArray());
        }
    }
}
