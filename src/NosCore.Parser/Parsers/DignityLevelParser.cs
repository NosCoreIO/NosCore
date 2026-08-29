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
    // Ceilings are derived from the next floor, not read: the client declares Useless ending at
    // -800 and Failed starting at -800, and only deriving gives Failed the -801 its packet states.
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

            var numbers = new List<short>();
            foreach (Match match in NumberPattern.Matches(range))
            {
                // A magnitude past short is a malformed band, not a crash.
                if (!short.TryParse(match.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
                {
                    return null;
                }

                numbers.Add(number);
            }

            return numbers.Count == 2 ? ((short)-numbers[0], (short)-numbers[1]) : null;
        }
    }
}
