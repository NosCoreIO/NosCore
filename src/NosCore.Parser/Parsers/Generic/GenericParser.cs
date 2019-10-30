using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FastMember;

namespace NosCore.Parser.Parsers.Generic
{

    public class GenericParser<T> where T : new()
    {
        private string _fileAddress;
        private string _endPattern;
        private TypeAccessor _typeAccessor;
        private Dictionary<string, Func<Dictionary<string, string[]>, object>> _actionList;
        private int _firstIndex;
        public GenericParser(string fileAddress, string endPattern, Dictionary<string, Func<Dictionary<string, string[]>, object>> actionList) => new GenericParser<T>(fileAddress, endPattern, 1, actionList);

        public GenericParser(string fileAddress, string endPattern, int firstIndex, Dictionary<string, Func<Dictionary<string, string[]>, object>> actionList)
        {
            _fileAddress = fileAddress;
            _endPattern = endPattern;
            _firstIndex = firstIndex;
            _typeAccessor = TypeAccessor.Create(typeof(T));
            _actionList = actionList;
        }

        private IEnumerable<string> ParseTextFromFile()
        {
            using (var stream = new StreamReader(_fileAddress, Encoding.Default))
            {
                var content = stream.ReadToEnd();
                return content.Split(_endPattern);
            }
        }


        public List<T> GetDtos()
        {
            var items = ParseTextFromFile();
            ConcurrentBag<T> resultCollection = new ConcurrentBag<T>();
            Parallel.ForEach(items, item =>
            {
                try
                {
                    var parsedItem = new T();
                    var lines = item.Split(Environment.NewLine.ToCharArray())
                        .Select(s => s.Split("   ")).ToDictionary(x => x[_firstIndex], y => y);
                    foreach (var actionOnKey in _actionList.Keys)
                    {
                        _typeAccessor[parsedItem, actionOnKey] = _actionList[actionOnKey].Invoke(lines);
                    }

                    resultCollection.Add(parsedItem);
                }
                catch
                {
                    //log
                    throw new InvalidDataException("Format of the parsed chunks invalid");
                }
            });
            return resultCollection.ToList();
        }
    }
}
