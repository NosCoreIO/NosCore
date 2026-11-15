//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.MiniMap;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    public class MapMonsterParser(IDao<MapMonsterDto, int> mapMonsterDao, IDao<NpcMonsterDto, short> npcMonsterDao,
        ILogger<MapMonsterParser> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        public async Task InsertMapMonsterAsync(IReadOnlyList<IPacket> packets)
        {
            short map = 0;
            var monsters = new List<MapMonsterDto>();
            var stored = mapMonsterDao.LoadAll().ToDictionary(s => s.MapMonsterId);
            var npcMonsterdb = npcMonsterDao.LoadAll().ToDictionary(n => n.NpcMonsterVNum);

            foreach (var packet in packets)
            {
                if (packet is AtPacket at)
                {
                    map = at.MapId;
                    continue;
                }

                if (packet is not InPacket { VisualType: VisualType.Monster, VisualId: <= 20000, InNonPlayerSubPacket: not null } inPacket
                    || !short.TryParse(inPacket.VNum, out var vnum) || !npcMonsterdb.ContainsKey(vnum))
                {
                    continue;
                }

                var monster = new MapMonsterDto
                {
                    MapId = map,
                    VNum = vnum,
                    MapMonsterId = (int)inPacket.VisualId,
                    MapX = inPacket.PositionX,
                    MapY = inPacket.PositionY,
                    Direction = inPacket.Direction ?? 0,
                    IsDisabled = false
                };

                if (monsters.Any(i => i.MapMonsterId == monster.MapMonsterId))
                {
                    continue;
                }

                monsters.Add(monster);
            }

            // A row stored on a captured monster's cell under another id is a copy from an earlier import.
            var ids = monsters.Select(s => s.MapMonsterId).ToHashSet();
            var cells = monsters.Select(s => (s.MapId, s.VNum, s.MapX, s.MapY)).ToHashSet();
            await mapMonsterDao.TryDeleteAsync(stored.Values
                .Where(s => !ids.Contains(s.MapMonsterId) && cells.Contains((s.MapId, s.VNum, s.MapX, s.MapY)))
                .Select(s => s.MapMonsterId).ToList());

            var changed = monsters.Where(s => !stored.TryGetValue(s.MapMonsterId, out var row)
                || (row.MapId, row.VNum, row.MapX, row.MapY, row.Direction, row.IsDisabled) != (s.MapId, s.VNum, s.MapX, s.MapY, s.Direction, s.IsDisabled)).ToList();
            await mapMonsterDao.TryInsertOrUpdateAsync(changed);
            logger.LogInformation(logLanguage[LogLanguageKey.MONSTERS_PARSED], changed.Count);
        }
    }
}
