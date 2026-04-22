//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers.Generic
{
    public class FluentParserBuilder<T> where T : new()
    {
        private readonly string _fileAddress;
        private readonly string _endPattern;
        private readonly int _firstIndex;
        private readonly Dictionary<string, Func<Dictionary<string, string[][]>, object?>> _actionList = new();
        private readonly List<DatFieldMetadata> _fields = new();
        private readonly List<DatSectionNote> _sectionNotes = new();
        private readonly List<DatSectionSchema> _sectionSchemas = new();
        private readonly List<DatColumnDoc> _columnDocs = new();
        private string _splitter = "\t";

        private FluentParserBuilder(string fileAddress, string endPattern, int firstIndex = 1)
        {
            _fileAddress = fileAddress;
            _endPattern = endPattern;
            _firstIndex = firstIndex;
        }

        public static FluentParserBuilder<T> Create(string fileAddress, string endPattern, int firstIndex = 1)
        {
            return new FluentParserBuilder<T>(fileAddress, endPattern, firstIndex);
        }

        // The canonical filename this builder targets (for doc generation output).
        public string FileName => Path.GetFileName(_fileAddress);

        // Parser-level index where the section tag sits in the split row. Data
        // columns start at `FirstIndex + 1`. Exposed so the doc generator can
        // distinguish parser plumbing from real .dat columns.
        public int FirstIndex => _firstIndex;

        public IReadOnlyList<DatFieldMetadata> Fields => _fields;
        public IReadOnlyList<DatSectionNote> SectionNotes => _sectionNotes;
        public IReadOnlyList<DatSectionSchema> SectionSchemas => _sectionSchemas;
        public IReadOnlyList<DatColumnDoc> ColumnDocs => _columnDocs;

        public FluentParserBuilder<T> WithSplitter(string splitter)
        {
            _splitter = splitter;
            return this;
        }

        public FluentParserBuilder<T> Field<TProperty>(
            Expression<Func<T, TProperty>> propertyExpression,
            Func<Dictionary<string, string[][]>, object?> extractor,
            string? source = null,
            string? description = null)
        {
            return Field(propertyExpression, extractor, Array.Empty<(string, int, int)>(), source, description);
        }

        public FluentParserBuilder<T> Field<TProperty>(
            Expression<Func<T, TProperty>> propertyExpression,
            Func<Dictionary<string, string[][]>, object?> extractor,
            (string Section, int Row, int Column)[] reads,
            string? source = null,
            string? description = null)
        {
            var propertyName = GetPropertyName(propertyExpression);
            _actionList[propertyName] = extractor;
            if (reads.Length == 0)
            {
                _fields.Add(new DatFieldMetadata(
                    propertyName, typeof(TProperty).Name,
                    Section: null, Row: null, Column: null,
                    Source: source, Description: description));
            }
            else
            {
                foreach (var (section, row, column) in reads)
                {
                    _fields.Add(new DatFieldMetadata(
                        propertyName, typeof(TProperty).Name,
                        section, row, column,
                        Source: source, Description: description));
                }
            }
            return this;
        }

        public FluentParserBuilder<T> Field<TProperty>(
            Expression<Func<T, TProperty>> propertyExpression,
            string section,
            int row,
            int column,
            string? description = null)
        {
            var propertyName = GetPropertyName(propertyExpression);
            _actionList[propertyName] = chunk => ConvertValue<TProperty>(chunk[section][row][column]);
            _fields.Add(new DatFieldMetadata(
                propertyName, typeof(TProperty).Name,
                section, row, column, Source: null, description));
            return this;
        }

        public FluentParserBuilder<T> Field<TProperty>(
            Expression<Func<T, TProperty>> propertyExpression,
            string section,
            int row,
            int column,
            Func<string, TProperty> converter,
            string? description = null)
        {
            var propertyName = GetPropertyName(propertyExpression);
            _actionList[propertyName] = chunk => converter(chunk[section][row][column]);
            _fields.Add(new DatFieldMetadata(
                propertyName, typeof(TProperty).Name,
                section, row, column, Source: null, description));
            return this;
        }

        // Section-level documentation. Call once per section that needs extra prose
        // (bitmask layouts, ID encodings, etc.).
        public FluentParserBuilder<T> Describe(string section, string description)
        {
            _sectionNotes.Add(new DatSectionNote(section, description));
            return this;
        }

        // Document a column the parser doesn't actually read. Zero effect on
        // parsing; the doc generator surfaces the `name` as the shape placeholder
        // and renders `description` alongside the parsed columns so reviewers
        // can see *what we know exists* vs *what we actually capture*.
        public FluentParserBuilder<T> Doc(string section, int column, string name, string? description = null)
        {
            _columnDocs.Add(new DatColumnDoc(section, column, name, description));
            return this;
        }

        // Declare how many columns the section is *supposed* to carry. Gap detection
        // flags any column not covered by a typed `.Field(...)` call.
        public FluentParserBuilder<T> ExpectedColumns(string section, int count, params string[] columnDescriptions)
        {
            _sectionSchemas.Add(new DatSectionSchema(section, count,
                columnDescriptions is { Length: > 0 } ? columnDescriptions : null));
            return this;
        }

        public FluentParser<T> Build(ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        {
            return new FluentParser<T>(_fileAddress, _endPattern, _firstIndex, _actionList, _splitter, logger, logLanguage);
        }

        private static string GetPropertyName<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            if (expression.Body is MemberExpression memberExpression)
            {
                return memberExpression.Member.Name;
            }
            throw new ArgumentException("Expression must be a member expression", nameof(expression));
        }

        private static object? ConvertValue<TProperty>(string value)
        {
            var targetType = typeof(TProperty);
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (underlyingType == typeof(string))
                return value;
            if (underlyingType == typeof(short))
                return Convert.ToInt16(value);
            if (underlyingType == typeof(int))
                return Convert.ToInt32(value);
            if (underlyingType == typeof(long))
                return Convert.ToInt64(value);
            if (underlyingType == typeof(byte))
                return Convert.ToByte(value);
            if (underlyingType == typeof(bool))
                return value == "1";
            if (underlyingType.IsEnum)
                return Enum.Parse(underlyingType, value);

            return Convert.ChangeType(value, underlyingType);
        }
    }

    public class FluentParser<T> where T : new()
    {
        private readonly GenericParser<T> _parser;
        private readonly string _splitter;

        internal FluentParser(
            string fileAddress,
            string endPattern,
            int firstIndex,
            Dictionary<string, Func<Dictionary<string, string[][]>, object?>> actionList,
            string splitter,
            ILogger logger,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        {
            _parser = new GenericParser<T>(fileAddress, endPattern, firstIndex, actionList, logger, logLanguage);
            _splitter = splitter;
        }

        public Task<List<T>> GetDtosAsync()
        {
            return _parser.GetDtosAsync(_splitter);
        }
    }
}
