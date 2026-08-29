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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    // Only the UK file is read: every language repeats the same numbers, but wrapped in
    // translated prose with its own separators and thousands marks, and CZ/PL are CP1250.
    public class ConstStringParser(IDao<ReputationLevelDto, byte> reputationLevelDao,
        IDao<DignityLevelDto, byte> dignityLevelDao, ILogger<ConstStringParser> logger,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        private const string FileName = "conststring_UK.dat";
        private const int FirstReputationKey = 2081;
        private const int ReputationBandCount = 27;
        private const int FirstDignityKey = 2111;
        private const int DignityBandCount = 6;

        private static readonly Regex NumberPattern = new(@"\d+", RegexOptions.Compiled);

        // The ranking places sharing this table read as plain numbers in French and German, so
        // only a letter separates them from a threshold - no numeric band has one, in any language.
        private static readonly Regex Letter = new(@"[^\W\d_]", RegexOptions.Compiled);

        public async Task InsertLaddersAsync(string folder)
        {
            var bands = await ReadBandsAsync(folder).ConfigureAwait(false);

            // Imported apart so a client that breaks one ladder still yields the other.
            await ImportAsync(BuildReputationLevels(bands), reputationLevelDao,
                LogLanguageKey.REPUTATIONLEVELS_PARSED, LogLanguageKey.REPUTATIONLEVELS_MALFORMED)
                .ConfigureAwait(false);
            await ImportAsync(BuildDignityLevels(bands), dignityLevelDao,
                LogLanguageKey.DIGNITYLEVELS_PARSED, LogLanguageKey.DIGNITYLEVELS_MALFORMED)
                .ConfigureAwait(false);
        }

        private async Task ImportAsync<TDto>(List<TDto>? levels, IDao<TDto, byte> dao,
            LogLanguageKey parsed, LogLanguageKey malformed)
        {
            if (levels == null)
            {
                logger.LogError(logLanguage[malformed]);
                return;
            }

            await dao.TryInsertOrUpdateAsync(levels).ConfigureAwait(false);
            logger.LogInformation(logLanguage[parsed], levels.Count);
        }

        private static async Task<Dictionary<int, string>> ReadBandsAsync(string folder)
        {
            // Latin1 never throws on any byte, and only digits are read out of the values.
            var content = await File.ReadAllTextAsync(Path.Combine(folder, FileName), Encoding.Latin1)
                .ConfigureAwait(false);

            var bands = new Dictionary<int, string>();
            foreach (var record in content.Split('\r', '\n'))
            {
                var separator = record.IndexOf('\v');
                if (separator <= 0)
                {
                    continue;
                }

                if (int.TryParse(record[..separator].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var key))
                {
                    bands[key] = record[(separator + 1)..];
                }
            }

            return bands;
        }

        // Reputation runs to 2110, but 2108..2110 are ranking places ("51st-100th") needing a
        // ranking NosCore does not keep, so only the 27 numeric bands are imported.
        private static List<ReputationLevelDto>? BuildReputationLevels(IReadOnlyDictionary<int, string> bands)
        {
            var levels = new List<ReputationLevelDto>(ReputationBandCount);
            long expectedMin = 0;

            for (var offset = 0; offset < ReputationBandCount; offset++)
            {
                if (!bands.TryGetValue(FirstReputationKey + offset, out var band))
                {
                    return null;
                }

                var numbers = ReadNumbers(band);

                // The highest band reads "Over 5000000": that number is the previous band's
                // ceiling, not its own floor.
                var isHighest = offset == ReputationBandCount - 1;
                if (!isHighest && Letter.IsMatch(band))
                {
                    return null;
                }

                if (numbers?.Count != (isHighest ? 1 : 2))
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

        // Ceilings are derived from the previous floor, not read: the client declares Useless
        // ending at -800 and Failed starting at -800, and only deriving gives Failed the -801
        // its packet documentation states.
        private static List<DignityLevelDto>? BuildDignityLevels(IReadOnlyDictionary<int, string> bands)
        {
            if (!bands.ContainsKey(FirstDignityKey))
            {
                return null;
            }

            var levels = new List<DignityLevelDto>(DignityBandCount)
            {
                new() { DignityLevelId = (byte)DignityType.Default, MaxDignity = null }
            };

            short? previousFloor = null;

            for (var offset = 1; offset < DignityBandCount; offset++)
            {
                if (!bands.TryGetValue(FirstDignityKey + offset, out var band))
                {
                    return null;
                }

                var bounds = ReadDignityBounds(band);
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
        private static (short Ceiling, short Floor)? ReadDignityBounds(string band)
        {
            var escape = band.IndexOf('#');
            var numbers = ReadNumbers(escape < 0 ? band : band[..escape]);

            if (numbers?.Count != 2 || numbers[0] > -short.MinValue || numbers[1] > -short.MinValue)
            {
                return null;
            }

            return ((short)-numbers[0], (short)-numbers[1]);
        }

        // A digit run too long for the target type is a malformed band, not a crash.
        private static List<long>? ReadNumbers(string band)
        {
            var numbers = new List<long>();
            foreach (Match match in NumberPattern.Matches(band))
            {
                if (!long.TryParse(match.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
                {
                    return null;
                }

                numbers.Add(number);
            }

            return numbers;
        }
    }
}
