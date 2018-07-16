using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NosCore.Data.AliveEntities;
using NosCore.Data.StaticEntities;
using NosCore.DAL;
using NosCore.Shared.I18N;

namespace NosCore.Parser.Parsers
{
    public class MapNpcParser
	{
		public void InsertMapNpcs(List<string[]> packetList)
		{
            int npcCounter = 0;
            short map = 0;
            List<MapNpcDTO> npcs = new List<MapNpcDTO>();
            List<int> npcMvPacketsList = new List<int>();
            Dictionary<int, short> effPacketsDictionary = new Dictionary<int, short>();

            foreach (string[] currentPacket in packetList.Where(o => o[0].Equals("mv") && o[1].Equals("2")))
            {
                if (long.Parse(currentPacket[2]) >= 20000)
                {
                    continue;
                }
                if (!npcMvPacketsList.Contains(Convert.ToInt32(currentPacket[2])))
                {
                    npcMvPacketsList.Add(Convert.ToInt32(currentPacket[2]));
                }
            }

            foreach (string[] currentPacket in packetList.Where(o => o[0].Equals("eff") && o[1].Equals("2")))
            {
                if (long.Parse(currentPacket[2]) >= 20000)
                {
                    continue;
                }
                if (!effPacketsDictionary.ContainsKey(Convert.ToInt32(currentPacket[2])))
                {
                    effPacketsDictionary.Add(Convert.ToInt32(currentPacket[2]), Convert.ToInt16(currentPacket[3]));
                }
            }

            foreach (string[] currentPacket in packetList.Where(o => o[0].Equals("in") || o[0].Equals("at")))
            {
                if (currentPacket.Length > 5 && currentPacket[0] == "at")
                {
                    map = short.Parse(currentPacket[2]);
                    continue;
                }
                if (currentPacket.Length <= 7 || currentPacket[0] != "in" || currentPacket[1] != "2")
                {
                    continue;
                }
                MapNpcDTO npctest = new MapNpcDTO
                {
                    MapX = short.Parse(currentPacket[4]),
                    MapY = short.Parse(currentPacket[5]),
                    MapId = map,
                    VNum = short.Parse(currentPacket[2])
                };
                if (long.Parse(currentPacket[3]) > 20000)
                {
                    continue;
                }
                npctest.MapNpcId = short.Parse(currentPacket[3]);
                if (effPacketsDictionary.ContainsKey(npctest.MapNpcId))
                {
                    npctest.Effect = effPacketsDictionary[npctest.MapNpcId];
                }
                npctest.EffectDelay = 4750;
                npctest.IsMoving = npcMvPacketsList.Contains(npctest.MapNpcId);
                npctest.Direction = byte.Parse(currentPacket[6]);
                npctest.Dialog = short.Parse(currentPacket[9]);
                npctest.IsSitting = currentPacket[13] != "1";
                npctest.IsDisabled = false;

                if (DAOFactory.NpcMonsterDAO.FirstOrDefault(s => s.NpcMonsterVNum.Equals(npctest.VNum)) == null || DAOFactory.MapNpcDAO.FirstOrDefault(s => s.MapNpcId.Equals(npctest.MapNpcId)) != null || npcs.Count(i => i.MapNpcId == npctest.MapNpcId) != 0)
                {
                    continue;
                }

                npcs.Add(npctest);
                npcCounter++;
            }
            IEnumerable<MapNpcDTO> npcDtos = npcs;
            DAOFactory.MapNpcDAO.InsertOrUpdate(npcDtos);
            Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.NPCS_PARSED), npcCounter));
        }
	}
}