using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TypeKitchen;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using Serilog;

namespace NosCore.Parser.Parsers.Generic
{

    public class GenericParser<T> where T : new()
    {
        private readonly ILogger _logger;
        private readonly string _fileAddress;
        private readonly string _endPattern;
        private readonly ITypeWriteAccessor _typeAccessor;
        private readonly Dictionary<string, Func<Dictionary<string, string[][]>, object?>> _actionList;
        private readonly int _firstIndex;

        public GenericParser(string fileAddress, string endPattern, int firstIndex, Dictionary<string, Func<Dictionary<string, string[][]>, object?>> actionList, ILogger logger)
        {
            _fileAddress = fileAddress;
            _endPattern = endPattern;
            _firstIndex = firstIndex;
            _typeAccessor = WriteAccessor.Create(typeof(T), AccessorMemberScope.All, out _);
            _actionList = actionList;
            _logger = logger;
        }

        private IEnumerable<string> ParseTextFromFile()
        {
            using var stream = new StreamReader(_fileAddress, Encoding.Default);
            var content = stream.ReadToEnd();
            return content.Split(_endPattern);
        }
        public List<T> GetDtos() => GetDtos("\t");
        public List<T> GetDtos(string splitter)
        {
            var items = ParseTextFromFile();
            ConcurrentBag<T> resultCollection = new ConcurrentBag<T>();
            Parallel.ForEach(items, new ParallelOptions
            {
                MaxDegreeOfParallelism = System.Diagnostics.Debugger.IsAttached ? 1 : -1
            }, item =>
            {
                var lines = item.Split(
                        new[] { "\r\n", "\r", "\n" },
                        StringSplitOptions.None
                    )
                    .Select(s => s.Split(splitter))
                    .Where(s => s.Length > _firstIndex)
                    .GroupBy(x => x[_firstIndex]).ToDictionary(x => x.Key, y => y.ToArray());
                if (lines.Count == 0)
                {
                    return;
                }
                try
                {
                    var parsedItem = new T();
                    foreach (var actionOnKey in _actionList.Keys)
                    {
                        try
                        {
                            _typeAccessor[parsedItem, actionOnKey] = _actionList[actionOnKey].Invoke(lines);
                        }
                        catch (Exception ex)
                        {
                            ex.Data.Add("actionKey", actionOnKey);
                            throw;
                        }
                    }

                    resultCollection.Add(parsedItem);
                }
                catch (Exception ex)
                {
                    _logger.Verbose(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CHUNK_FORMAT_INVALID), lines, ex);
                }
            });
            return resultCollection.ToList();
        }
    }
}
