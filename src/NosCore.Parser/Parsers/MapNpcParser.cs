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
            var mapnpcdb = mapNpcDao.LoadAll().ToList();
            var npcCounter = 0;
            short map = 0;
            var npcs = new List<MapNpcDto>();
            var effPacketsDictionary = packetList.Where(o => o[0].Equals("eff") && o[1].Equals("2") && long.Parse(o[2]) <= 20000).GroupBy(s => Convert.ToInt16(s[2])).ToDictionary(x => x.Key, x => Convert.ToInt16(x.First()[3]));
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
                    Effect = effPacketsDictionary.TryGetValue(mapnpcid, out var value) ? value : (short)0,
                    EffectDelay = 4750,
                    IsMoving = npcMonster.CanWalk,
                    Direction = byte.Parse(currentPacket[6]),
                    Dialog = npcTalks.ContainsKey(short.Parse(currentPacket[9])) ? short.Parse(currentPacket[9]) : (short?)null,
                    IsSitting = currentPacket[13] != "1",
                    IsDisabled = false
                };

                if ((mapnpcdb.FirstOrDefault(s => s.MapNpcId.Equals(npctest.MapNpcId)) !=
                        null)
                    || (npcs.Count(i => i.MapNpcId == npctest.MapNpcId) != 0))
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
