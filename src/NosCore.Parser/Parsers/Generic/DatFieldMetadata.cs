//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.Parser.Parsers.Generic
{
    // Column-level metadata attached to each `.Field(...)` call in a parser builder.
    // The doc generator reads these to emit per-section tables and to diff against
    // the curated `ExpectedColumns` schema, surfacing .dat columns we don't read.
    public sealed record DatFieldMetadata(
        string PropertyName,
        string PropertyTypeName,
        string? Section,
        int? Row,
        int? Column,
        string? Source,
        string? Description);

    // Free-form note attached to a whole section (e.g. ETC bitmask semantics).
    public sealed record DatSectionNote(string Section, string Description);

    // A column the parser knows about but doesn't read. Lets us carry NSgtd-style
    // documentation on unparsed columns so the shape header still names them
    // (`{DisappearAfterSeconds}` instead of `0`) and the section table explains
    // their purpose.
    public sealed record DatColumnDoc(
        string Section,
        int Column,
        string Name,
        string? Description);

    // Curated claim about how many columns a section is expected to carry, for gap
    // detection. `ColumnDescriptions[i]` is the human-readable purpose of column i.
    public sealed record DatSectionSchema(
        string Section,
        int ExpectedColumnCount,
        string[]? ColumnDescriptions = null);
}
