using NosCore.DAL;
using NosCore.Data.AliveEntities;
using NosCore.Shared.I18N;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    public class MapMonsterParser
    {

        public void ImportMonsters(List<string[]> packetList)
        {
            short map = 0;
            ConcurrentBag<int> mobMvPacketsList = new ConcurrentBag<int>();
            List<MapMonsterDTO> monsters = new List<MapMonsterDTO>();

            Parallel.ForEach(packetList.Where(o => o[0].Equals("mv") && o[1].Equals("3")), currentPacket =>
            {
                if (!mobMvPacketsList.Contains(int.Parse(currentPacket[2])))
                {
                    mobMvPacketsList.Add(int.Parse(currentPacket[2]));
                }
            });
            foreach (string[] currentPacket in packetList.Where(o => o[0].Equals("in") || o[0].Equals("at")))
            {
                if (currentPacket.Length > 5 && currentPacket[0] == "at")
                {
                    map = short.Parse(currentPacket[2]);
                    continue;
                }
                if (currentPacket.Length > 7 && currentPacket[0] == "in" && currentPacket[1] == "3")
                {
                    MapMonsterDTO monster = new MapMonsterDTO
                    {
                        MapId = map,
                        VNum = short.Parse(currentPacket[2]),
                        MapMonsterId = int.Parse(currentPacket[3]),
                        MapX = short.Parse(currentPacket[4]),
                        MapY = short.Parse(currentPacket[5]),
                        Direction = (byte)(currentPacket[6]?.Length == 0 ? 0 : byte.Parse(currentPacket[6])),
                        IsDisabled = false
                    };
                    monster.IsMoving = mobMvPacketsList.Contains(monster.MapMonsterId);
                    if (DAOFactory.NpcMonsterDAO.FirstOrDefault(s => s.NpcMonsterVNum.Equals(monster.VNum)) == null || DAOFactory.MapMonsterDAO.FirstOrDefault(s => s.MapMonsterId.Equals(monster.MapMonsterId)) != null || monsters.Any(i => i.MapMonsterId == monster.MapMonsterId))
                    {
                        continue;
                    }
                    monsters.Add(monster);
                }
            }
            DAOFactory.MapMonsterDAO.InsertOrUpdate(monsters);
            Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.MAPMONSTER_PARSED), monsters.Count));
        }
    }
}
