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
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    public class MapMonsterParser(IDao<MapMonsterDto, int> mapMonsterDao, IDao<NpcMonsterDto, short> npcMonsterDao,
        ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        public async Task InsertMapMonsterAsync(List<string[]> packetList)
        {
            short map = 0;
            var mobMvPacketsList = packetList.Where(o => o[0].Equals("mv") && o[1].Equals("3"))
                .Select(currentPacket => Convert.ToInt32(currentPacket[2])).Distinct().ToList();
            var monsters = new List<MapMonsterDto>();
            var mapMonsterdb = mapMonsterDao.LoadAll().ToList();
            var npcMonsterdb = npcMonsterDao.LoadAll().ToList();

            foreach (var currentPacket in packetList.Where(o => (o.Length > 7 && o[0].Equals("in") && (o[1] == "3") && long.Parse(o[3]) <= 20000) || o[0].Equals("at")))
            {
                if ((currentPacket.Length > 5) && (currentPacket[0] == "at"))
                {
                    map = short.Parse(currentPacket[2]);
                    continue;
                }

                var monster = new MapMonsterDto
                {
                    MapId = map,
                    VNum = short.Parse(currentPacket[2]),
                    MapMonsterId = int.Parse(currentPacket[3]),
                    MapX = short.Parse(currentPacket[4]),
                    MapY = short.Parse(currentPacket[5]),
                    Direction = (byte)(currentPacket[6] == string.Empty ? 0 : byte.Parse(currentPacket[6])),
                    IsDisabled = false,
                    IsMoving = mobMvPacketsList.Contains(int.Parse(currentPacket[3]))
                };

                if ((npcMonsterdb.FirstOrDefault(s => s.NpcMonsterVNum.Equals(monster.VNum)) == null)
                    || (mapMonsterdb.FirstOrDefault(s => s.MapMonsterId.Equals(monster.MapMonsterId)) != null)
                    || (monsters.Count(i => i.MapMonsterId == monster.MapMonsterId) != 0))
                {
                    continue;
                }

                monsters.Add(monster);
            }

            await mapMonsterDao.TryInsertOrUpdateAsync(monsters);
            logger.Information(logLanguage[LogLanguageKey.MONSTERS_PARSED],
                monsters.Count);
        }
    }
}
