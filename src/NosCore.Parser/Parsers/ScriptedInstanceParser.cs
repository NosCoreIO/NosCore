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
            var known = scriptedInstanceDao.LoadAll()
                .Select(s => (s.MapId, s.PositionX, s.PositionY)).ToHashSet();
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
                if (!known.Add(key))
                {
                    return;
                }

                found.Add(entrance);
            }
        }
    }
}
