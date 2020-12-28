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
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Packets.Enumerations;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    public class PortalParser
    {
        private readonly List<PortalDto> _listPortals2 = new List<PortalDto>();
        private readonly ILogger _logger;
        private readonly IDao<MapDto, short> _mapDao;
        private readonly IDao<PortalDto, int> _portalDao;
        private List<PortalDto> _listPortals1 = new List<PortalDto>();

        public PortalParser(ILogger logger, IDao<MapDto, short> mapDao, IDao<PortalDto, int> portalDao)
        {
            _logger = logger;
            _mapDao = mapDao;
            _portalDao = portalDao;
        }

        public async Task InsertPortalsAsync(List<string[]> packetList)
        {
            var portalsdb = _portalDao.LoadAll().ToList();
            var maps = _mapDao.LoadAll().ToList();
            short map = 0;
            var portalCounter = 0;
            var lodPortal = new PortalDto
            {
                SourceMapId = 150,
                SourceX = 172,
                SourceY = 171,
                DestinationMapId = 98,
                Type = PortalType.MapPortal,
                DestinationX = 6,
                DestinationY = 36,
                IsDisabled = false
            };
            var portalsave4 = lodPortal;
            if (portalsdb.FirstOrDefault(s => s.SourceMapId == portalsave4.SourceMapId) ==
                null)
            {
                portalCounter++;
                await _portalDao.TryInsertOrUpdateAsync(lodPortal).ConfigureAwait(false);
            }

            var minilandPortal = new PortalDto
            {
                SourceMapId = 20001,
                SourceX = 3,
                SourceY = 8,
                DestinationMapId = 1,
                Type = PortalType.MapPortal,
                DestinationX = 48,
                DestinationY = 132,
                IsDisabled = false
            };

            var portalsave3 = minilandPortal;
            if (portalsdb.FirstOrDefault(s => s.SourceMapId == portalsave3.SourceMapId) ==
                null)
            {
                portalCounter++;
                await _portalDao.TryInsertOrUpdateAsync(minilandPortal).ConfigureAwait(false);
            }

            var weddingPortal = new PortalDto
            {
                SourceMapId = 2586,
                SourceX = 34,
                SourceY = 54,
                DestinationMapId = 145,
                Type = PortalType.MapPortal,
                DestinationX = 61,
                DestinationY = 165,
                IsDisabled = false
            };
            var portalsave2 = weddingPortal;
            if (portalsdb.FirstOrDefault(s => s.SourceMapId == portalsave2.SourceMapId) ==
                null)
            {
                portalCounter++;
                await _portalDao.TryInsertOrUpdateAsync(weddingPortal).ConfigureAwait(false);
            }

            var glacerusCavernPortal = new PortalDto
            {
                SourceMapId = 2587,
                SourceX = 42,
                SourceY = 3,
                DestinationMapId = 189,
                Type = PortalType.MapPortal,
                DestinationX = 48,
                DestinationY = 156,
                IsDisabled = false
            };
            var portalsave1 = glacerusCavernPortal;
            if (portalsdb.FirstOrDefault(s => s.SourceMapId == portalsave1.SourceMapId) ==
                null)
            {
                portalCounter++;
                await _portalDao.TryInsertOrUpdateAsync(glacerusCavernPortal).ConfigureAwait(false);
            }

            foreach (var currentPacket in packetList.Where(o => o[0].Equals("at") || o[0].Equals("gp")))
            {
                if ((currentPacket.Length > 5) && (currentPacket[0] == "at"))
                {
                    map = short.Parse(currentPacket[2]);
                    continue;
                }

                if ((currentPacket.Length <= 4) || (currentPacket[0] != "gp"))
                {
                    continue;
                }

                var portal = new PortalDto
                {
                    SourceMapId = map,
                    SourceX = short.Parse(currentPacket[1]),
                    SourceY = short.Parse(currentPacket[2]),
                    DestinationMapId = short.Parse(currentPacket[3]),
                    Type = (PortalType)Enum.Parse(typeof(PortalType), currentPacket[4]),
                    DestinationX = -1,
                    DestinationY = -1,
                    IsDisabled = false
                };

                if (_listPortals1.Any(s =>
                        (s.SourceMapId == map) && (s.SourceX == portal.SourceX) && (s.SourceY == portal.SourceY)
                        && (s.DestinationMapId == portal.DestinationMapId))
                    || maps.All(s => s.MapId != portal.SourceMapId)
                    || maps.All(s => s.MapId != portal.DestinationMapId))
                {
                    // Portal already in list
                    continue;
                }

                _listPortals1.Add(portal);
            }

            _listPortals1 = _listPortals1.OrderBy(s => s.SourceMapId).ThenBy(s => s.DestinationMapId)
                .ThenBy(s => s.SourceY).ThenBy(s => s.SourceX).ToList();
            foreach (var portal in _listPortals1)
            {
                var p = _listPortals1.Except(_listPortals2).FirstOrDefault(s =>
                    (s.SourceMapId == portal.DestinationMapId) && (s.DestinationMapId == portal.SourceMapId));
                if (p == null)
                {
                    continue;
                }

                portal.DestinationX = p.SourceX;
                portal.DestinationY = p.SourceY;
                p.DestinationY = portal.SourceY;
                p.DestinationX = portal.SourceX;
                _listPortals2.Add(p);
                _listPortals2.Add(portal);
            }


            // so this dude doesnt exist yet in DAOFactory -> insert it
            var portalsDtos = _listPortals2.Where(portal => !portalsdb
                .Where(s => s.SourceMapId.Equals(portal.SourceMapId)).Any(
                    s => (s.DestinationMapId == portal.DestinationMapId) && (s.SourceX == portal.SourceX)
                        && (s.SourceY == portal.SourceY))).ToList();
            await _portalDao.TryInsertOrUpdateAsync(portalsDtos).ConfigureAwait(false);

            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PORTALS_PARSED),
                portalsDtos.Count + portalCounter);
        }
    }
}