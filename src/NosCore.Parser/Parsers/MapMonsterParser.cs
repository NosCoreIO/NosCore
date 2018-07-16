using NosCore.DAL;
using NosCore.Data.AliveEntities;
using NosCore.Shared.I18N;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.Parser.Parsers
{
    internal class MapMonsterParser
    {
        public void InsertMapMonster(List<string[]> packetList)
        {
            int monsterCounter = 0;
            short map = 0;
            List<int> mobMvPacketsList = new List<int>();
            List<MapMonsterDTO> monsters = new List<MapMonsterDTO>();

            foreach (string[] currentPacket in packetList.Where(o => o[0].Equals("mv") && o[1].Equals("3")))
            {
                if (!mobMvPacketsList.Contains(Convert.ToInt32(currentPacket[2])))
                {
                    mobMvPacketsList.Add(Convert.ToInt32(currentPacket[2]));
                }
            }

            foreach (string[] currentPacket in packetList.Where(o => o[0].Equals("in") || o[0].Equals("at")))
            {
                if (currentPacket.Length > 5 && currentPacket[0] == "at")
                {
                    map = short.Parse(currentPacket[2]);
                    continue;
                }
                if (currentPacket.Length <= 7 || currentPacket[0] != "in" || currentPacket[1] != "3")
                {
                    continue;
                }
                MapMonsterDTO monster = new MapMonsterDTO
                {
                    MapId = map,
                    VNum = short.Parse(currentPacket[2]),
                    MapMonsterId = int.Parse(currentPacket[3]),
                    MapX = short.Parse(currentPacket[4]),
                    MapY = short.Parse(currentPacket[5]),
                    Direction = (byte)(currentPacket[6] == string.Empty ? 0 : byte.Parse(currentPacket[6])),
                    IsDisabled = false
                };
                monster.IsMoving = mobMvPacketsList.Contains(monster.MapMonsterId);

                if (DAOFactory.NpcMonsterDAO.FirstOrDefault(s => s.NpcMonsterVNum.Equals(monster.VNum)) == null || DAOFactory.MapMonsterDAO.FirstOrDefault(s => s.MapMonsterId.Equals(monster.MapMonsterId)) != null || monsters.Count(i => i.MapMonsterId == monster.MapMonsterId) != 0)
                {
                    continue;
                }

                monsters.Add(monster);
                monsterCounter++;
            }


            IEnumerable<MapMonsterDTO> mapMonsterDtos = monsters;
            DAOFactory.MapMonsterDAO.InsertOrUpdate(mapMonsterDtos);
            Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.MONSTERS_PARSED), monsterCounter));
        }
    }
}