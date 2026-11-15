//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.MiniMap;
using NosCore.Packets.ServerPackets.Portals;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    // Portals on a map the capture shows are the ones the capture pairs there, so a rerun drops the
    // ones a newer capture no longer shows and leaves the database as a fresh import would.
    public class PortalParser(ILogger<PortalParser> logger, IDao<MapDto, short> mapDao, IDao<PortalDto, int> portalDao, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        // Portals the capture cannot show: they lead somewhere only a script or an NPC sends the player to.
        private static PortalDto[] Unseen() =>
        [
            new() { SourceMapId = 150, SourceX = 172, SourceY = 171, DestinationMapId = 98, DestinationX = 6, DestinationY = 36, Type = PortalType.MapPortal },
            new() { SourceMapId = 20001, SourceX = 3, SourceY = 8, DestinationMapId = 1, DestinationX = 48, DestinationY = 132, Type = PortalType.MapPortal },
            new() { SourceMapId = 2586, SourceX = 34, SourceY = 54, DestinationMapId = 145, DestinationX = 61, DestinationY = 165, Type = PortalType.MapPortal },
            new() { SourceMapId = 2587, SourceX = 42, SourceY = 3, DestinationMapId = 189, DestinationX = 48, DestinationY = 156, Type = PortalType.MapPortal },
            // The arenas are entered through an NPC, so no portal leads out of them.
            new() { SourceMapId = 2006, SourceX = 37, SourceY = 15, DestinationMapId = 1, DestinationX = 50, DestinationY = 66, Type = PortalType.MapPortal },
            new() { SourceMapId = 2106, SourceX = 38, SourceY = 3, DestinationMapId = 1, DestinationX = 50, DestinationY = 66, Type = PortalType.MapPortal },
        ];

        // A miniland entrance leads to the miniland of whoever walks in, so the server only takes its
        // position and the miniland map from here.
        private static PortalDto[] MinilandEntrances() =>
        [
            new() { SourceMapId = 1, SourceX = 48, SourceY = 132, DestinationMapId = 20001, DestinationX = 5, DestinationY = 8, Type = PortalType.Miniland },
            new() { SourceMapId = 145, SourceX = 9, SourceY = 171, DestinationMapId = 20001, DestinationX = 5, DestinationY = 8, Type = PortalType.Miniland },
        ];

        public async Task InsertPortalsAsync(IReadOnlyList<IPacket> packets)
        {
            var maps = mapDao.LoadAll().Select(s => s.MapId).ToHashSet();
            var seen = new List<PortalDto>();
            short map = 0;
            foreach (var packet in packets)
            {
                if (packet is AtPacket at)
                {
                    map = at.MapId;
                    continue;
                }

                if (packet is not GpPacket gp || !maps.Contains(map) || !maps.Contains(gp.MapId)
                    || seen.Any(s => s.SourceMapId == map && s.SourceX == gp.SourceX && s.SourceY == gp.SourceY && s.DestinationMapId == gp.MapId))
                {
                    continue;
                }

                seen.Add(new PortalDto
                {
                    SourceMapId = map,
                    SourceX = gp.SourceX,
                    SourceY = gp.SourceY,
                    DestinationMapId = gp.MapId,
                    Type = gp.PortalType,
                    DestinationX = -1,
                    DestinationY = -1,
                });
            }

            // A portal is kept once the capture also shows one leading back, whose cell is where it lands.
            var paired = new List<PortalDto>();
            seen = seen.OrderBy(s => s.SourceMapId).ThenBy(s => s.DestinationMapId).ThenBy(s => s.SourceY).ThenBy(s => s.SourceX).ToList();
            foreach (var portal in seen)
            {
                var back = seen.Except(paired).FirstOrDefault(s => s.SourceMapId == portal.DestinationMapId && s.DestinationMapId == portal.SourceMapId);
                if (back == null)
                {
                    continue;
                }

                portal.DestinationX = back.SourceX;
                portal.DestinationY = back.SourceY;
                back.DestinationX = portal.SourceX;
                back.DestinationY = portal.SourceY;
                paired.Add(back);
                paired.Add(portal);
            }

            var desired = paired
                .Concat(Unseen().Where(s => maps.Contains(s.SourceMapId) && paired.All(p => p.SourceMapId != s.SourceMapId)))
                .Concat(MinilandEntrances())
                .GroupBy(Key).ToDictionary(g => g.Key, g => g.First());

            var stored = portalDao.LoadAll().ToList();
            var shown = CaptureSessions.Maps(packets);
            var kept = new HashSet<int>();
            var changed = new List<PortalDto>();
            foreach (var (key, portal) in desired)
            {
                var row = stored.FirstOrDefault(s => !kept.Contains(s.PortalId) && Key(s) == key);
                if (row != null)
                {
                    kept.Add(row.PortalId);
                    if (Fields(row).Equals(Fields(portal)))
                    {
                        continue;
                    }

                    portal.PortalId = row.PortalId;
                }

                changed.Add(portal);
            }

            await portalDao.TryDeleteAsync(stored.Where(s => !kept.Contains(s.PortalId)
                && (shown.Contains(s.SourceMapId) || desired.ContainsKey(Key(s)))).Select(s => s.PortalId).ToList());
            await portalDao.TryInsertOrUpdateAsync(changed);
            logger.LogInformation(logLanguage[LogLanguageKey.PORTALS_PARSED], changed.Count);
        }

        private static (short, short, short, short) Key(PortalDto portal) => (portal.SourceMapId, portal.SourceX, portal.SourceY, portal.DestinationMapId);

        private static object Fields(PortalDto portal) => (portal.DestinationX, portal.DestinationY, portal.Type, portal.IsDisabled);
    }
}
