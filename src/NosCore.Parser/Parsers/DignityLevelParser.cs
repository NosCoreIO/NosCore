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
    // Dignity is the tail of the same conststring table the reputation ladder comes from: names
    // at 2075..2080, bands at 2111..2116, matching DignityType 1..6. The bands read
    // "100 - 0", "-100 ~ -200", "-201 to -400" and so on, each of the penalty ones followed by
    // the effects text after a #13#10 escape.
    //
    // Only each band's floor is trusted. The ceilings are derived as one past the previous floor
    // because the client contradicts itself once: Useless ends at -800 and Failed is declared as
    // starting at -800 too, which would put -800 in two bands. The client's own packet
    // documentation puts Failed at -801, and that is what deriving produces.
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

        // Every penalty band is negative, so the two magnitudes are negated. The effects text is
        // cut first because it carries digits of its own ("10% price increase").
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
