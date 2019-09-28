//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using Serilog;

namespace NosCore.Parser.Parsers
{
    public class MapNpcParser
    {
        private readonly ILogger _logger;
        private readonly IGenericDao<MapNpcDto> _mapNpcDao;
        private readonly IGenericDao<NpcMonsterDto> _npcMonsterDao;

        public MapNpcParser(IGenericDao<MapNpcDto> mapNpcDao, IGenericDao<NpcMonsterDto> npcMonsterDao, ILogger logger)
        {
            _mapNpcDao = mapNpcDao;
            _logger = logger;
            _npcMonsterDao = npcMonsterDao;
        }

        public void InsertMapNpcs(List<string[]> packetList)
        {
            var npcmonsterdb = _npcMonsterDao.LoadAll().ToList();
            var mapnpcdb = _mapNpcDao.LoadAll().ToList();
            var npcCounter = 0;
            short map = 0;
            var npcs = new List<MapNpcDto>();
            var npcMvPacketsList = new List<int>();
            var effPacketsDictionary = new Dictionary<int, short>();

            foreach (var currentPacket in packetList.Where(o => o[0].Equals("mv") && o[1].Equals("2")))
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

            foreach (var currentPacket in packetList.Where(o => o[0].Equals("eff") && o[1].Equals("2")))
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

            foreach (var currentPacket in packetList.Where(o => o[0].Equals("in") || o[0].Equals("at")))
            {
                if ((currentPacket.Length > 5) && (currentPacket[0] == "at"))
                {
                    map = short.Parse(currentPacket[2]);
                    continue;
                }

                if ((currentPacket.Length <= 7) || (currentPacket[0] != "in") || (currentPacket[1] != "2"))
                {
                    continue;
                }

                var npctest = new MapNpcDto
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

                if ((npcmonsterdb.FirstOrDefault(s => s.NpcMonsterVNum.Equals(npctest.VNum)) == null)
                    || (mapnpcdb.FirstOrDefault(s => s.MapNpcId.Equals(npctest.MapNpcId)) !=
                        null)
                    || (npcs.Count(i => i.MapNpcId == npctest.MapNpcId) != 0))
                {
                    continue;
                }

                npcs.Add(npctest);
                npcCounter++;
            }

            IEnumerable<MapNpcDto> npcDtos = npcs;
            _mapNpcDao.InsertOrUpdate(npcDtos);
            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.NPCS_PARSED), npcCounter);
        }
    }
}