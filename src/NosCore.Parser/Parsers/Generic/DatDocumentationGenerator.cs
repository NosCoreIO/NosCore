//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NosCore.Parser.Parsers.Generic
{
    // Emits a NSgtd-style Markdown dump of a builder's metadata.
    //
    // Layout:
    //   1. Shape template — one line per section, tab-indented. Columns render as
    //      `{PropertyName}` for parsed fields, `{DocName}` for documented-not-read
    //      columns, or `0` for columns we have no metadata on.
    //   2. Per-section tables — column / name / type / description.
    //   3. Computed / multi-section fields — lambdas without a single (section, col).
    public static class DatDocumentationGenerator
    {
        public static string Generate<T>(FluentParserBuilder<T> builder) where T : new()
        {
            // Parser plumbing (leading empty + section tag) ends at builder.FirstIndex;
            // real .dat data columns start at the next slot. Skip the plumbing in the
            // rendered shape and in the "not read" / "without metadata" tables.
            var firstDataColumn = builder.FirstIndex + 1;

            var sb = new StringBuilder();
            sb.Append("# ").AppendLine(builder.FileName);
            sb.AppendLine();

            // Schema derives from the parser's Field calls themselves. A section
            // exists in the doc iff at least one Field reads it; each section's
            // column count is inferred as `max(column)+1` from those reads. That
            // means the "columns not read" gap list only covers the range we have
            // evidence about — we can't flag columns or whole sections we've never
            // seen without reading the actual .dat file.
            var fieldsBySection = builder.Fields
                .Where(f => f.Section != null)
                .GroupBy(f => f.Section!)
                .ToDictionary(g => g.Key, g => g.ToList());
            var docsBySection = builder.ColumnDocs
                .GroupBy(d => d.Section)
                .ToDictionary(g => g.Key, g => g.ToList());
            var notesBySection = builder.SectionNotes
                .GroupBy(n => n.Section)
                .ToDictionary(g => g.Key, g => g.Select(n => n.Description).ToList());

            var orderedSections = fieldsBySection.Keys
                .Union(docsBySection.Keys)
                .OrderBy(s => s)
                .ToList();

            // Pass 1 — build the shape template. Placeholder is always the
            // DTO property name (for parsed columns) or the Doc name (for
            // documented-not-read columns); the per-section tables below
            // carry the description so we don't need a separate References block.
            var shape = new StringBuilder();
            shape.AppendLine("```");
            foreach (var section in orderedSections)
            {
                shape.Append('\t').Append(section);
                fieldsBySection.TryGetValue(section, out var sectionFields);
                docsBySection.TryGetValue(section, out var sectionDocs);

                var maxField = sectionFields?.Max(f => f.Column ?? -1) ?? -1;
                var maxDoc = sectionDocs?.Max(d => d.Column) ?? -1;
                var columnCount = Math.Max(maxField, maxDoc) + 1;

                for (var col = firstDataColumn; col < columnCount; col++)
                {
                    var match = sectionFields?.FirstOrDefault(f => f.Column == col);
                    var doc = sectionDocs?.FirstOrDefault(d => d.Column == col);
                    string placeholder;
                    if (match != null)
                    {
                        placeholder = $"{{{match.PropertyName}}}";
                    }
                    else if (doc != null)
                    {
                        placeholder = $"{{{doc.Name}}}";
                    }
                    else
                    {
                        // Matches the existing parser comment-header convention: `0`
                        // for columns we don't currently read but fall within the
                        // inferred section extent.
                        placeholder = "0";
                    }
                    shape.Append('\t').Append(placeholder);
                }
                shape.AppendLine();
            }
            shape.AppendLine("```");
            sb.Append(shape);
            sb.AppendLine();

            // Pass 2 — per-section tables.
            foreach (var section in orderedSections)
            {
                fieldsBySection.TryGetValue(section, out var sectionFields);
                docsBySection.TryGetValue(section, out var sectionDocs);
                if ((sectionFields == null || sectionFields.Count == 0)
                    && (sectionDocs == null || sectionDocs.Count == 0))
                {
                    continue;
                }

                sb.Append("## ").AppendLine(section);
                if (notesBySection.TryGetValue(section, out var notes))
                {
                    foreach (var note in notes) sb.AppendLine(note);
                    sb.AppendLine();
                }

                sb.AppendLine("| Column | Status | Name | Type | Description |");
                sb.AppendLine("|---:|---|---|---|---|");
                var covered = new HashSet<int>();
                var parsedRows = (sectionFields?.Select(f => (Col: f.Column ?? -1,
                    Status: "Parsed", Name: f.PropertyName, Type: f.PropertyTypeName,
                    Desc: f.Description ?? "")) ?? Enumerable.Empty<(int, string, string, string, string)>()).ToList();
                var parsedCols = new HashSet<int>(parsedRows.Where(r => r.Col >= 0).Select(r => r.Col));
                var docRows = sectionDocs?
                    .Where(d => !parsedCols.Contains(d.Column))
                    .Select(d => (Col: d.Column,
                        Status: "NonParsed", Name: d.Name, Type: "",
                        Desc: d.Description ?? "")) ?? Enumerable.Empty<(int, string, string, string, string)>();
                var namedRows = parsedRows.Concat(docRows).ToList();
                foreach (var row in namedRows) { if (row.Col >= 0) covered.Add(row.Col); }

                var maxKnownColumn = covered.Count > 0 ? covered.Max() : firstDataColumn - 1;
                var unknownRows = Enumerable.Range(firstDataColumn, maxKnownColumn - firstDataColumn + 1)
                    .Where(i => !covered.Contains(i))
                    .Select(i => (Col: i, Status: "Unknown", Name: "", Type: "", Desc: ""));

                foreach (var row in namedRows.Concat(unknownRows).OrderBy(r => r.Col).ThenBy(r => r.Name))
                {
                    sb.Append("| ").Append(row.Col >= 0 ? row.Col.ToString() : "-")
                        .Append(" | ").Append(row.Status)
                        .Append(" | ").Append(row.Name)
                        .Append(" | ").Append(row.Type)
                        .Append(" | ").Append(row.Desc).AppendLine(" |");
                }

                sb.AppendLine();
            }

            // Pass 3 — computed / multi-section.
            var computed = builder.Fields.Where(f => f.Section == null).ToList();
            if (computed.Count > 0)
            {
                sb.AppendLine("## Computed / multi-section fields");
                sb.AppendLine("| DTO property | Type | Source | Description |");
                sb.AppendLine("|---|---|---|---|");
                foreach (var f in computed.OrderBy(f => f.PropertyName))
                {
                    sb.Append("| ").Append(f.PropertyName)
                        .Append(" | ").Append(f.PropertyTypeName)
                        .Append(" | ").Append(f.Source ?? "")
                        .Append(" | ").Append(f.Description ?? "").AppendLine(" |");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
