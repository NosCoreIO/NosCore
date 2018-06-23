using NosCore.DAL;
using NosCore.Data.StaticEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NosCore.Parser.Parsers
{
    public class NpcMonsterDataParser
    {
        public void InserNpcMonsterData(List<string[]> packetList)
        {
            foreach (string[] currentPacket in packetList.Where(o => o[0].Equals("e_info") && o[1].Equals("10")))
            {
                if (currentPacket.Length <= 25)
                {
                    continue;
                }
                var npcvnum = short.Parse(currentPacket[2]);
                NpcMonsterDTO npcMonster = DAOFactory.NpcMonsterDAO.FirstOrDefault(s => s.NpcMonsterVNum == npcvnum);
                if (npcMonster == null)
                {
                    continue;
                }
                npcMonster.AttackClass = byte.Parse(currentPacket[5]);
                npcMonster.AttackUpgrade = byte.Parse(currentPacket[7]);
                npcMonster.DamageMinimum = short.Parse(currentPacket[8]);
                npcMonster.DamageMaximum = short.Parse(currentPacket[9]);
                npcMonster.Concentrate = short.Parse(currentPacket[10]);
                npcMonster.CriticalChance = byte.Parse(currentPacket[11]);
                npcMonster.CriticalRate = short.Parse(currentPacket[12]);
                npcMonster.DefenceUpgrade = byte.Parse(currentPacket[13]);
                npcMonster.CloseDefence = short.Parse(currentPacket[14]);
                npcMonster.DefenceDodge = short.Parse(currentPacket[15]);
                npcMonster.DistanceDefence = short.Parse(currentPacket[16]);
                npcMonster.DistanceDefenceDodge = short.Parse(currentPacket[17]);
                npcMonster.MagicDefence = short.Parse(currentPacket[18]);
                npcMonster.FireResistance = sbyte.Parse(currentPacket[19]);
                npcMonster.WaterResistance = sbyte.Parse(currentPacket[20]);
                npcMonster.LightResistance = sbyte.Parse(currentPacket[21]);
                npcMonster.DarkResistance = sbyte.Parse(currentPacket[22]);

                DAOFactory.NpcMonsterDAO.InsertOrUpdate(ref npcMonster);
            }
        }
    }
}
