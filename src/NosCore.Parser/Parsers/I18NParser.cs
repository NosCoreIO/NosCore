//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    public class I18NParser<TDto, TPk>(IDao<TDto, TPk> dao, ILogger logger,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    where TDto : II18NDto, new()
    where TPk : struct
    {
        private string I18NTextFileName(string textfilename, RegionType region)
        {
            var regioncode = region.ToString().ToLower();
            regioncode = regioncode == "en" ? "uk" : regioncode == "cs" ? "cz" : regioncode;
            return string.Format(textfilename, regioncode);
        }

        public Task InsertI18NAsync(string file, LogLanguageKey logLanguageKey)
        {
            var listoftext = dao.LoadAll().ToDictionary(x => (x.Key, x.RegionType), x => x.Text);

            return Task.WhenAll(((RegionType[])Enum.GetValues(typeof(RegionType))).Select(async region =>
            {
                var dtos = new Dictionary<string, TDto>();
                try
                {
                    using var stream = new StreamReader(I18NTextFileName(file, region),
                        Encoding.Default);
                    var lines = (await stream.ReadToEndAsync()).Split(
                        new[] { "\r\n", "\r", "\n" },
                        StringSplitOptions.None
                    );
                    foreach (var line in lines)
                    {
                        var currentLine = line.Split('\t');
                        if (currentLine.Length > 1 && !listoftext.ContainsKey((currentLine[0], region)) &&
                            !dtos.ContainsKey(currentLine[0]))
                        {
                            dtos.Add(currentLine[0], new TDto()
                            {
                                Key = currentLine[0],
                                RegionType = region,
                                Text = currentLine[1],
                            });
                        }
                    }
                    await dao.TryInsertOrUpdateAsync(dtos.Values);

                    logger.Information(
                        logLanguage[logLanguageKey],
                        dtos.Count,
                        region);
                }
                catch (FileNotFoundException)
                {
                    logger.Warning(logLanguage[LogLanguageKey.LANGUAGE_MISSING]);
                }
            }));
        }
    }
}
