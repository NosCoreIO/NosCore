//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    // The client keeps the reputation ladder in conststring as two parallel contiguous tables:
    // names at 2045..2080 and the matching bands at 2081..2116. The first 30 of each are
    // reputation and the last 6 are dignity. Of the 30 reputation bands only the first 27 are
    // numeric — 2108..2110 are ranking places ("51st-100th"), which need a server-side ranking
    // NosCore does not keep. Those first 27 line up one-for-one with ReputationType 1..27.
    //
    // Only the UK file is read. The same numbers appear in all nine languages, but wrapped in
    // translated prose with per-language separators ("-", "~", "to"), thousands separators
    // (ES/RU write "1.001"), and CZ/PL are CP1250 rather than CP1252.
    public class ReputationLevelParser(IDao<ReputationLevelDto, byte> reputationLevelDao,
        ILogger<ReputationLevelParser> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        private const string FileName = "conststring_UK.dat";
        private const int FirstBandKey = 2081;
        private const int NumericBandCount = 27;

        private static readonly Regex NumberPattern = new(@"\d+", RegexOptions.Compiled);

        public async Task InsertReputationLevelsAsync(string folder)
        {
            var path = Path.Combine(folder, FileName);
            // Latin1 round-trips every byte, so a client shipping a different codepage cannot
            // make the read throw. Only digit runs are consumed from the values.
            var content = await File.ReadAllTextAsync(path, Encoding.Latin1).ConfigureAwait(false);

            var bands = ReadBands(content);
            var levels = BuildLevels(bands);
            if (levels == null)
            {
                logger.LogError(logLanguage[LogLanguageKey.REPUTATIONLEVELS_MALFORMED]);
                return;
            }

            await reputationLevelDao.TryInsertOrUpdateAsync(levels).ConfigureAwait(false);
            logger.LogInformation(logLanguage[LogLanguageKey.REPUTATIONLEVELS_PARSED], levels.Count);
        }

        private static Dictionary<int, string> ReadBands(string content)
        {
            var bands = new Dictionary<int, string>();
            foreach (var record in content.Split('\r', '\n'))
            {
                var separator = record.IndexOf('\v');
                if (separator <= 0)
                {
                    continue;
                }

                if (!int.TryParse(record[..separator].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var key))
                {
                    continue;
                }

                if (key >= FirstBandKey && key < FirstBandKey + NumericBandCount)
                {
                    bands[key] = record[(separator + 1)..];
                }
            }

            return bands;
        }

        private static List<ReputationLevelDto>? BuildLevels(IReadOnlyDictionary<int, string> bands)
        {
            var levels = new List<ReputationLevelDto>(NumericBandCount);
            long expectedMin = 0;

            for (var offset = 0; offset < NumericBandCount; offset++)
            {
                if (!bands.TryGetValue(FirstBandKey + offset, out var band))
                {
                    return null;
                }

                var numbers = NumberPattern.Matches(band)
                    .Select(match => long.Parse(match.Value, CultureInfo.InvariantCulture))
                    .ToList();

                // The highest band reads "Over 5000000": its single number is the previous
                // band's upper bound, and it has no upper bound of its own.
                var isHighest = offset == NumericBandCount - 1;
                if (numbers.Count != (isHighest ? 1 : 2))
                {
                    return null;
                }

                if (isHighest)
                {
                    if (numbers[0] != expectedMin - 1)
                    {
                        return null;
                    }
                }
                else if (numbers[0] != expectedMin || numbers[1] <= numbers[0])
                {
                    return null;
                }

                levels.Add(new ReputationLevelDto
                {
                    ReputationLevelId = (byte)(ReputationType)(offset + 1),
                    MinReputation = expectedMin,
                    MaxReputation = isHighest ? null : numbers[1]
                });

                expectedMin = isHighest ? expectedMin : numbers[1] + 1;
            }

            return levels;
        }
    }
}
