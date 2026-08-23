//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.StaticEntities;
using NosCore.Packets.Enumerations;
using NosCore.Shared.I18N;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    public class ScriptedInstanceParser(ILogger<ScriptedInstanceParser> logger,
        IDao<MapDto, short> mapDao, IDao<ScriptedInstanceDto, short> scriptedInstanceDao,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        private static readonly PortalType[] RaidPortals =
            [PortalType.Raid, PortalType.BlueRaid, PortalType.DarkRaid];

        public async Task InsertScriptedInstancesAsync(List<string[]> packetList)
        {
            var maps = mapDao.LoadAll().Select(s => s.MapId).ToHashSet();
            var stored = scriptedInstanceDao.LoadAll()
                .ToDictionary(s => (s.MapId, s.PositionX, s.PositionY), s => s);
            var seen = new HashSet<(short, short, short)>();
            var found = new List<ScriptedInstanceDto>();
            short currentMap = 0;

            foreach (var line in packetList)
            {
                switch (line[0])
                {
                    case "at" when line.Length > 5:
                        currentMap = short.Parse(line[2], CultureInfo.InvariantCulture);
                        continue;

                    case "wp" when line.Length > 6:
                        Collect(new ScriptedInstanceDto
                        {
                            MapId = currentMap,
                            PositionX = short.Parse(line[1], CultureInfo.InvariantCulture),
                            PositionY = short.Parse(line[2], CultureInfo.InvariantCulture),
                            Type = ScriptedInstanceType.TimeSpace,
                            IsHeroic = (byte.Parse(line[4], CultureInfo.InvariantCulture) & 8) != 0,
                            LevelMinimum = byte.Parse(line[5], CultureInfo.InvariantCulture),
                            LevelMaximum = byte.Parse(line[6], CultureInfo.InvariantCulture)
                        });
                        continue;

                    case "gp" when line.Length > 4:
                        if (!System.Enum.TryParse<PortalType>(line[4], out var portalType)
                            || !RaidPortals.Contains(portalType))
                        {
                            continue;
                        }

                        Collect(new ScriptedInstanceDto
                        {
                            MapId = currentMap,
                            PositionX = short.Parse(line[1], CultureInfo.InvariantCulture),
                            PositionY = short.Parse(line[2], CultureInfo.InvariantCulture),
                            Type = ScriptedInstanceType.Raid
                        });
                        continue;
                }
            }

            await scriptedInstanceDao.TryInsertOrUpdateAsync(found).ConfigureAwait(false);
            logger.LogInformation(logLanguage[LogLanguageKey.TIMESPACES_PARSED], found.Count);
            return;

            void Collect(ScriptedInstanceDto entrance)
            {
                if (!maps.Contains(entrance.MapId))
                {
                    return;
                }

                var key = (entrance.MapId, entrance.PositionX, entrance.PositionY);

                // Within one run the same entrance can turn up more than once - the capture walks
                // a map twice and the wp comes round again. First reading wins.
                if (!seen.Add(key))
                {
                    return;
                }

                // An entrance already in the table gets its metadata refreshed rather than
                // skipped. Skipping it looked safe and was not: the columns this parser exists to
                // fill arrive from the migration as 0, 0 and false, so on any database that
                // already holds entrances the import would leave every level range at zero and
                // every raid non-heroic - and say nothing.
                //
                // The id and the script come from the stored row: the id so the upsert updates
                // instead of inserting a duplicate, the script because it is not ours to write.
                if (stored.TryGetValue(key, out var existing))
                {
                    entrance.ScriptedInstanceId = existing.ScriptedInstanceId;
                    entrance.Script = existing.Script;
                }

                found.Add(entrance);
            }
        }
    }
}
