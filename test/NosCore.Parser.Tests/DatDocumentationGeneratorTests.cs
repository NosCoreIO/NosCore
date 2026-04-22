//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Parser.Parsers.Generic;

namespace NosCore.Parser.Tests
{
    // Lightweight sanity check for the .dat doc generator. Real parsers migrate to
    // the metadata-aware overloads incrementally; this test exercises the infra
    // with a toy DTO so the API + output shape stay verified without pulling in a
    // full filesystem + DAO stack.
    [TestClass]
    public class DatDocumentationGeneratorTests
    {
        private sealed class ToyDto
        {
            public short VNum { get; set; }
            public long Price { get; set; }
            public bool Flag9 { get; set; }
            public byte Computed { get; set; }
        }

        [TestMethod]
        public void GeneratedMarkdownIncludesShapeSectionTablesAndComputed()
        {
            var builder = FluentParserBuilder<ToyDto>.Create("anywhere/Item.dat", "END")
                .Field(x => x.VNum, "VNUM", 0, 2, s => System.Convert.ToInt16(s),
                    description: "Item vnum")
                .Field(x => x.Price, "VNUM", 0, 3, s => System.Convert.ToInt64(s),
                    description: "Shop sell price")
                .Field(x => x.Flag9, "FLAG", 0, 10, s => s == "1",
                    description: "FLAG bit 9")
                .Field(x => x.Computed, _ => (byte)0,
                    source: "FLAG[25] + DATA[0]", description: "Demo computed field")
                .Describe("FLAG", "25 boolean bits; each column is a single toggle.")
                .Doc("FLAG", 9, "Warehouseable", "Column the parser knows about but doesn't read.");

            var md = DatDocumentationGenerator.Generate(builder);

            StringAssert.Contains(md, "# Item.dat");
            StringAssert.Contains(md, "\tVNUM\t{VNum}\t{Price}");
            StringAssert.Contains(md, "{Flag9}");
            StringAssert.Contains(md, "{Warehouseable}");
            StringAssert.Contains(md, "## FLAG");
            StringAssert.Contains(md, "25 boolean bits; each column");
            StringAssert.Contains(md, "| 10 | Parsed | Flag9 | Boolean | FLAG bit 9 |");
            StringAssert.Contains(md, "| 9 | NonParsed | Warehouseable |  | Column the parser knows about but doesn't read. |");
            StringAssert.Contains(md, "| 2 | Unknown |"); // column gap in FLAG
            StringAssert.Contains(md, "## Computed / multi-section fields");
            StringAssert.Contains(md, "| Computed | Byte | FLAG[25] + DATA[0] | Demo computed field |");
        }
    }
}
