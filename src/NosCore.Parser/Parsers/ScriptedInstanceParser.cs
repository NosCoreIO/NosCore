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
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.MiniMap;
using NosCore.Packets.ServerPackets.Portals;
using NosCore.Shared.I18N;
using System.Collections.Generic;
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

        public async Task InsertScriptedInstancesAsync(IReadOnlyList<IPacket> packets)
        {
            var maps = mapDao.LoadAll().Select(s => s.MapId).ToHashSet();
            var stored = scriptedInstanceDao.LoadAll()
                .ToDictionary(s => (s.MapId, s.PositionX, s.PositionY), s => s);
            var seen = new HashSet<(short, short, short)>();
            var found = new List<ScriptedInstanceDto>();
            short currentMap = 0;

            foreach (var packet in packets)
            {
                switch (packet)
                {
                    case AtPacket at:
                        currentMap = at.MapId;
                        continue;

                    case WpPacket wp:
                        Collect(new ScriptedInstanceDto
                        {
                            MapId = currentMap,
                            PositionX = wp.PositionX,
                            PositionY = wp.PositionY,
                            Type = ScriptedInstanceType.TimeSpace,
                            IsHeroic = ((byte)wp.PortalType & 8) != 0,
                            LevelMinimum = wp.LevelMinimum,
                            LevelMaximum = wp.LevelMaximum
                        });
                        continue;

                    case GpPacket gp when RaidPortals.Contains(gp.PortalType):
                        Collect(new ScriptedInstanceDto
                        {
                            MapId = currentMap,
                            PositionX = gp.SourceX,
                            PositionY = gp.SourceY,
                            Type = ScriptedInstanceType.Raid
                        });
                        continue;
                }
            }

            await scriptedInstanceDao.TryInsertOrUpdateAsync(found);
            logger.LogInformation(logLanguage[LogLanguageKey.TIMESPACES_PARSED], found.Count);
            return;

            void Collect(ScriptedInstanceDto entrance)
            {
                if (!maps.Contains(entrance.MapId))
                {
                    return;
                }

                var key = (entrance.MapId, entrance.PositionX, entrance.PositionY);

                if (!seen.Add(key))
                {
                    return;
                }

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
