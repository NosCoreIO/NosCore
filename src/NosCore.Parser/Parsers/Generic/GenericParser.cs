//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using FastMember;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers.Generic
{

    public class GenericParser<T>(string fileAddress, string endPattern, int firstIndex,
        Dictionary<string, Func<Dictionary<string, string[][]>, object?>> actionList, ILogger logger,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    where T : new()
    {
        private readonly TypeAccessor _typeAccessor = TypeAccessor.Create(typeof(T), true);

        private IEnumerable<string> ParseTextFromFile()
        {
            using var stream = new StreamReader(fileAddress, Encoding.Default);
            var content = stream.ReadToEnd();
            var i = 0;
            return content.Split(endPattern).Select(s => $"{(i++ == 0 ? "" : endPattern)}{s}");
        }
        public Task<List<T>> GetDtosAsync() => GetDtosAsync("\t");
        public async Task<List<T>> GetDtosAsync(string splitter)
        {
            var items = ParseTextFromFile();
            ConcurrentBag<T> resultCollection = new ConcurrentBag<T>();
            await Task.WhenAll(items.Select(item => Task.Run(() =>
            {
                var lines = item.Split(
                        new[] { "\r\n", "\r", "\n" },
                        StringSplitOptions.None
                    )
                    .Select(s => s.Split(splitter))
                    .Where(s => s.Length > firstIndex)
                    .GroupBy(x => x[firstIndex]).ToDictionary(x => x.Key, y => y.ToArray());
                if (lines.Count == 0)
                {
                    return;
                }
                try
                {
                    var parsedItem = new T();
                    foreach (var actionOnKey in actionList.Keys)
                    {
                        try
                        {
                            _typeAccessor[parsedItem, actionOnKey] = actionList[actionOnKey].Invoke(lines);
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
                    logger.Verbose(logLanguage[LogLanguageKey.CHUNK_FORMAT_INVALID], lines, ex);
                }
            })));
            return resultCollection.ToList();
        }
    }
}
