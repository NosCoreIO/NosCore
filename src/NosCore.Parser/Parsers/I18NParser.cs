//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NosCore.Core.I18N;
using NosCore.Dao.Interfaces;
using NosCore.Data;
using NosCore.Data.Enumerations.I18N;
using NosCore.Packets.Enumerations;
using Serilog;

namespace NosCore.Parser.Parsers
{
    public class I18NParser<TDto, TPk> where TDto : II18NDto, new() where TPk : struct
    {
        private readonly ILogger _logger;
        private readonly IDao<TDto, TPk> _dao;
        public I18NParser(IDao<TDto, TPk> dao, ILogger logger)
        {
            _dao = dao;
            _logger = logger;
        }

        private string I18NTextFileName(string textfilename, RegionType region)
        {
            var regioncode = region.ToString().ToLower();
            regioncode = regioncode == "en" ? "uk" : regioncode;
            return string.Format(textfilename, regioncode);
        }

        public Task InsertI18NAsync(string file, LogLanguageKey logLanguageKey)
        {
            var listoftext = _dao.LoadAll().ToDictionary(x => (x.Key, x.RegionType), x => x.Text);

            return Task.WhenAll(((RegionType[])Enum.GetValues(typeof(RegionType))).Select(async region =>
            {
                var dtos = new Dictionary<string, TDto>();
                try
                {
                    using var stream = new StreamReader(I18NTextFileName(file, region),
                        Encoding.Default);
                    var lines = (await stream.ReadToEndAsync().ConfigureAwait(false)).Split(
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
                    await _dao.TryInsertOrUpdateAsync(dtos.Values).ConfigureAwait(false);

                    _logger.Information(string.Format(
                        LogLanguage.Instance.GetMessageFromKey(logLanguageKey),
                        dtos.Count,
                        region));
                }
                catch (FileNotFoundException)
                {
                    _logger.Warning(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.LANGUAGE_MISSING));
                }
            }));
        }
    }
}