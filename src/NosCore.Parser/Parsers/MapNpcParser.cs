//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
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
        public async Task InsertMapNpcsAsync(List<string[]> packetList)
        {
            var npcmonsterdb = npcMonsterDao.LoadAll().ToDictionary(n => n.NpcMonsterVNum);
            var npcCounter = 0;
            short map = 0;
            var npcs = new List<MapNpcDto>();
            var npcTalks = npcTalkDao.LoadAll().ToDictionary(s => s.DialogId, s => s);
            foreach (var currentPacket in packetList.Where(o => (o.Length > 7 && o[0].Equals("in") && (o[1] == "2") && long.Parse(o[3]) <= 20000) || o[0].Equals("at")))
            {
                if ((currentPacket.Length > 5) && (currentPacket[0] == "at"))
                {
                    map = short.Parse(currentPacket[2]);
                    continue;
                }

                var mapnpcid = short.Parse(currentPacket[3]);
                var vnum = short.Parse(currentPacket[2]);
                if (!npcmonsterdb.TryGetValue(vnum, out var npcMonster))
                {
                    continue;
                }

                var npctest = new MapNpcDto
                {
                    MapX = short.Parse(currentPacket[4]),
                    MapY = short.Parse(currentPacket[5]),
                    MapId = map,
                    VNum = vnum,
                    MapNpcId = mapnpcid,
                    Effect = npcMonster.EffectIdConstantly,
                    EffectDelay = 4750,
                    IsMoving = npcMonster.CanWalk,
                    Direction = byte.Parse(currentPacket[6]),
                    Dialog = npcTalks.ContainsKey(short.Parse(currentPacket[9])) ? short.Parse(currentPacket[9]) : (short?)null,
                    IsSitting = currentPacket[13] != "1",
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
