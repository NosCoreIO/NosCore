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

using NosCore.Core.I18N;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    public class MapNpcParser
    {
        private readonly ILogger _logger;
        private readonly IDao<MapNpcDto, int> _mapNpcDao;
        private readonly IDao<NpcMonsterDto, short> _npcMonsterDao;
        private readonly IDao<NpcTalkDto, short> _npcTalkDao;

        public MapNpcParser(IDao<MapNpcDto, int> mapNpcDao, IDao<NpcMonsterDto, short> npcMonsterDao, IDao<NpcTalkDto, short> npcTalkDao, ILogger logger)
        {
            _mapNpcDao = mapNpcDao;
            _logger = logger;
            _npcMonsterDao = npcMonsterDao;
            _npcTalkDao = npcTalkDao;
        }

        public async Task InsertMapNpcsAsync(List<string[]> packetList)
        {
            var npcmonsterdb = _npcMonsterDao.LoadAll().ToList();
            var mapnpcdb = _mapNpcDao.LoadAll().ToList();
            var npcCounter = 0;
            short map = 0;
            var npcs = new List<MapNpcDto>();
            var npcMvPacketsList = packetList.Where(o => o.Length > 14 && o[0].Equals("mv") && o[1].Equals("2") && long.Parse(o[2]) < 20000).GroupBy(s => s[2]).Select(s => Convert.ToInt32(s.First()[2])).ToList();
            var effPacketsDictionary = packetList.Where(o => o[0].Equals("eff") && o[1].Equals("2") && long.Parse(o[2]) <= 20000).GroupBy(s => Convert.ToInt16(s[2])).ToDictionary(x => x.Key, x => Convert.ToInt16(x.First()[3]));
            var npcTalks = _npcTalkDao.LoadAll().ToDictionary(s => s.DialogId, s => s);
            foreach (var currentPacket in packetList.Where(o => (o.Length > 7 && o[0].Equals("in") && (o[1] == "2") && long.Parse(o[3]) <= 20000) || o[0].Equals("at")))
            {
                if ((currentPacket.Length > 5) && (currentPacket[0] == "at"))
                {
                    map = short.Parse(currentPacket[2]);
                    continue;
                }

                var mapnpcid = short.Parse(currentPacket[3]);
                var npctest = new MapNpcDto
                {
                    MapX = short.Parse(currentPacket[4]),
                    MapY = short.Parse(currentPacket[5]),
                    MapId = map,
                    VNum = short.Parse(currentPacket[2]),
                    MapNpcId = mapnpcid,
                    Effect = effPacketsDictionary.ContainsKey(mapnpcid) ? effPacketsDictionary[mapnpcid] : (short)0,
                    EffectDelay = 4750,
                    IsMoving = npcMvPacketsList.Contains(mapnpcid),
                    Direction = byte.Parse(currentPacket[6]),
                    Dialog = npcTalks.ContainsKey(short.Parse(currentPacket[9])) ? short.Parse(currentPacket[9]) : (short?)null,
                    IsSitting = currentPacket[13] != "1",
                    IsDisabled = false
                };

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

            await _mapNpcDao.TryInsertOrUpdateAsync(npcs).ConfigureAwait(false);
            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.NPCS_PARSED), npcCounter);
        }
    }
}