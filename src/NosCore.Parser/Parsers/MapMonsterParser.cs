using System;
using System.Collections.Generic;
using System.Linq;
using NosCore.Data.AliveEntities;
using NosCore.DAL;
using NosCore.Shared.I18N;

namespace NosCore.Parser.Parsers
{
    internal class MapMonsterParser
    {
        public void InsertMapMonster(List<string[]> packetList)
        {
            var monsterCounter = 0;
            short map = 0;
            var mobMvPacketsList = new List<int>();
            var monsters = new List<MapMonsterDTO>();

            foreach (var currentPacket in packetList.Where(o => o[0].Equals("mv") && o[1].Equals("3")))
            {
                if (!mobMvPacketsList.Contains(Convert.ToInt32(currentPacket[2])))
                {
                    mobMvPacketsList.Add(Convert.ToInt32(currentPacket[2]));
                }
            }

            foreach (var currentPacket in packetList.Where(o => o[0].Equals("in") || o[0].Equals("at")))
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

                var monster = new MapMonsterDTO
                {
                    MapId = map,
                    VNum = short.Parse(currentPacket[2]),
                    MapMonsterId = int.Parse(currentPacket[3]),
                    MapX = short.Parse(currentPacket[4]),
                    MapY = short.Parse(currentPacket[5]),
                    Direction = (byte) (currentPacket[6] == string.Empty ? 0 : byte.Parse(currentPacket[6])),
                    IsDisabled = false
                };
                monster.IsMoving = mobMvPacketsList.Contains(monster.MapMonsterId);

                if (DAOFactory.NpcMonsterDAO.FirstOrDefault(s => s.NpcMonsterVNum.Equals(monster.VNum)) == null ||
                    DAOFactory.MapMonsterDAO.FirstOrDefault(s => s.MapMonsterId.Equals(monster.MapMonsterId)) != null ||
                    monsters.Count(i => i.MapMonsterId == monster.MapMonsterId) != 0)
                {
                    continue;
                }

                monsters.Add(monster);
                monsterCounter++;
            }


            IEnumerable<MapMonsterDTO> mapMonsterDtos = monsters;
            DAOFactory.MapMonsterDAO.InsertOrUpdate(mapMonsterDtos);
            Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.MONSTERS_PARSED),
                monsterCounter));
        }
    }
}