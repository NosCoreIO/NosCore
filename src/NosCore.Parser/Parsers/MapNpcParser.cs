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
    public class MapNpcParser(IDao<MapNpcDto, int> mapNpcDao, IDao<NpcMonsterDto, short> npcMonsterDao, IDao<NpcTalkDto, short> npcTalkDao,
        ILogger<MapNpcParser> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        public async Task InsertMapNpcsAsync(IReadOnlyList<IPacket> packets)
        {
            var npcmonsterdb = npcMonsterDao.LoadAll().ToDictionary(n => n.NpcMonsterVNum);
            var npcCounter = 0;
            short map = 0;
            var npcs = new List<MapNpcDto>();
            var npcTalks = npcTalkDao.LoadAll().ToDictionary(s => s.DialogId, s => s);
            foreach (var packet in packets)
            {
                if (packet is AtPacket at)
                {
                    map = at.MapId;
                    continue;
                }

                if (packet is not InPacket { VisualType: VisualType.Npc, VisualId: <= 20000, InNonPlayerSubPacket: { } npc } inPacket
                    || !short.TryParse(inPacket.VNum, out var vnum) || !npcmonsterdb.TryGetValue(vnum, out var npcMonster))
                {
                    continue;
                }

                var npctest = new MapNpcDto
                {
                    MapX = inPacket.PositionX,
                    MapY = inPacket.PositionY,
                    MapId = map,
                    VNum = vnum,
                    MapNpcId = (int)inPacket.VisualId,
                    Effect = npcMonster.EffectIdConstantly,
                    EffectDelay = 4750,
                    IsMoving = npcMonster.CanWalk,
                    Direction = inPacket.Direction ?? 0,
                    Dialog = npcTalks.ContainsKey(npc.Dialog) ? npc.Dialog : null,
                    IsSitting = npc.IsSitting,
                    IsDisabled = false
                };

                if (npcs.Any(i => i.MapNpcId == npctest.MapNpcId))
                {
                    continue;
                }

                npcs.Add(npctest);
                npcCounter++;
            }

            await mapNpcDao.TryInsertOrUpdateAsync(npcs);
            logger.LogInformation(logLanguage[LogLanguageKey.NPCS_PARSED], npcCounter);
        }
    }
}
