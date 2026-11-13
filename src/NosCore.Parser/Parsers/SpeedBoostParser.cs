//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.StaticEntities;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    // What a Speed Booster does on a mount is written only in the mount's English description, under
    // "[With Speed Booster]", one effect per line until the next bracketed heading.
    public partial class SpeedBoostParser(IDao<ItemDto, short> itemDao, ILogger<SpeedBoostParser> logger)
    {
        private const string Heading = "[With Speed Booster]";
        private const short RandomTeleportEffect = 3625;

        [GeneratedRegex(@"\bspeed:? \+(\d+)", RegexOptions.IgnoreCase)]
        private static partial Regex Speed();

        [GeneratedRegex(@"\bduration:? \+(\d+)", RegexOptions.IgnoreCase)]
        private static partial Regex Duration();

        [GeneratedRegex(@"\brestores HP and MP \(your level \*(\d+)\)", RegexOptions.IgnoreCase)]
        private static partial Regex RestoresHpAndMp();

        [GeneratedRegex(@"\b(?:bad effects|negative effects|debuffs) (?:up to|under|below) level (\d+)", RegexOptions.IgnoreCase)]
        private static partial Regex RemovesDebuffs();

        [GeneratedRegex(@"\bteleports you to a random point\b", RegexOptions.IgnoreCase)]
        private static partial Regex TeleportsRandomly();

        public async Task InsertSpeedBoostsAsync(string folder)
        {
            var descriptions = ItemDescriptions.Read(folder);
            var boosted = new List<ItemDto>();
            foreach (var item in itemDao.LoadAll())
            {
                if (!descriptions.TryGetValue(item.VNum, out var description) || !description.Contains(Heading))
                {
                    continue;
                }

                var effects = string.Join(' ', description[(description.IndexOf(Heading) + Heading.Length)..]
                    .Split("[n]")
                    .Select(l => l.Trim())
                    .TakeWhile(l => !l.StartsWith('[')));

                item.SpeedBoost = Number(Speed(), effects);
                item.SpeedBoostDuration = Number(Duration(), effects);
                item.SpeedBoostRestoresPerLevel = Number(RestoresHpAndMp(), effects);
                item.SpeedBoostRemovesDebuffsUpToLevel = Number(RemovesDebuffs(), effects);
                item.SpeedBoostTeleportsRandomly = TeleportsRandomly().IsMatch(effects);
                item.SpeedBoostTeleportEffect = item.SpeedBoostTeleportsRandomly ? RandomTeleportEffect : (short)0;
                boosted.Add(item);
            }

            await itemDao.TryInsertOrUpdateAsync(boosted);
            logger.LogInformation("{Mounts} mounts take their Speed Booster effect from their description", boosted.Count);
        }

        private static byte Number(Regex pattern, string text) =>
            pattern.Match(text) is { Success: true } match && byte.TryParse(match.Groups[1].Value, out var value) ? value : (byte)0;
    }
}
