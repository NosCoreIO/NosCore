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

        public FluentParserBuilder<T> WithSplitter(string splitter)
        {
            _splitter = splitter;
            return this;
        }

        public FluentParserBuilder<T> Field<TProperty>(
            Expression<Func<T, TProperty>> propertyExpression,
            Func<Dictionary<string, string[][]>, object?> extractor)
        {
            var propertyName = GetPropertyName(propertyExpression);
            _actionList[propertyName] = extractor;
            return this;
        }

        public FluentParserBuilder<T> Field<TProperty>(
            Expression<Func<T, TProperty>> propertyExpression,
            string section,
            int row,
            int column)
        {
            var propertyName = GetPropertyName(propertyExpression);
            _actionList[propertyName] = chunk => ConvertValue<TProperty>(chunk[section][row][column]);
            return this;
        }

        public FluentParserBuilder<T> Field<TProperty>(
            Expression<Func<T, TProperty>> propertyExpression,
            string section,
            int row,
            int column,
            Func<string, TProperty> converter)
        {
            var propertyName = GetPropertyName(propertyExpression);
            _actionList[propertyName] = chunk => converter(chunk[section][row][column]);
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
