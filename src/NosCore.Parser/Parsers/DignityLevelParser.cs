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
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    // Bands 2111..2116 are the dignity tail of the same table, matching DignityType 1..6.
    //
    // Only each band's floor is trusted, ceilings being derived as one past the previous floor,
    // because the client contradicts itself once: Useless ends at -800 and Failed is declared to
    // start at -800 as well. Deriving gives Failed the -801 its own packet documentation states.
    public class DignityLevelParser(IDao<DignityLevelDto, byte> dignityLevelDao,
        ILogger<DignityLevelParser> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        private const int DefaultBandKey = 2111;
        private const int BandCount = 6;

        private static readonly Regex NumberPattern = new(@"\d+", RegexOptions.Compiled);

        public async Task InsertDignityLevelsAsync(string folder)
        {
            var bands = await ConstStringFile.ReadAsync(folder).ConfigureAwait(false);
            var levels = BuildLevels(bands);
            if (levels == null)
            {
                logger.LogError(logLanguage[LogLanguageKey.DIGNITYLEVELS_MALFORMED]);
                return;
            }

            await dignityLevelDao.TryInsertOrUpdateAsync(levels).ConfigureAwait(false);
            logger.LogInformation(logLanguage[LogLanguageKey.DIGNITYLEVELS_PARSED], levels.Count);
        }

        private static List<DignityLevelDto>? BuildLevels(IReadOnlyDictionary<int, string> bands)
        {
            if (!bands.ContainsKey(DefaultBandKey))
            {
                return null;
            }

            var levels = new List<DignityLevelDto>(BandCount)
            {
                new() { DignityLevelId = (byte)DignityType.Default, MaxDignity = null }
            };

            short? previousFloor = null;

            for (var offset = 1; offset < BandCount; offset++)
            {
                if (!bands.TryGetValue(DefaultBandKey + offset, out var band))
                {
                    return null;
                }

                var bounds = ReadBounds(band);
                if (bounds == null)
                {
                    return null;
                }

                var (declaredCeiling, floor) = bounds.Value;
                var ceiling = previousFloor == null ? declaredCeiling : (short)(previousFloor.Value - 1);

                if (floor >= ceiling || (previousFloor != null && floor >= previousFloor.Value))
                {
                    return null;
                }

                levels.Add(new DignityLevelDto
                {
                    DignityLevelId = (byte)(DignityType)(offset + 1),
                    MaxDignity = ceiling
                });

                previousFloor = floor;
            }

            return levels;
        }

        // Penalty bands are negative, so the magnitudes are negated. The effects text is cut
        // first because it carries digits of its own ("-201 to -400#13#1010% price increase").
        private static (short Ceiling, short Floor)? ReadBounds(string band)
        {
            var escape = band.IndexOf('#');
            var range = escape < 0 ? band : band[..escape];

            var numbers = NumberPattern.Matches(range)
                .Select(match => short.Parse(match.Value, CultureInfo.InvariantCulture))
                .ToList();

            return numbers.Count == 2 ? ((short)-numbers[0], (short)-numbers[1]) : null;
        }
    }
}
