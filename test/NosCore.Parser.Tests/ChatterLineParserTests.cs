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
            Assert.IsTrue(saved.All(line => (line.Message == null) != (line.Text == null)));
            CollectionAssert.Contains(
                saved.Where(line => line.ChatterGroup == 2).Select(line => line.Text).ToArray(),
                nameof(LanguageKey.CHATTER_IMPROVE_WORKING_CONDITIONS));
            CollectionAssert.Contains(
                saved.Where(line => line.ChatterGroup == 1).Select(line => line.Message).ToArray(),
                (short?)(short)Game18NConstString.KeeeKeeee);
        }
    }
}
