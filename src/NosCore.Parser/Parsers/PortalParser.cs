using NosCore.DAL;
using NosCore.Data.StaticEntities;
using NosCore.Shared.Enumerations.Map;
using NosCore.Shared.I18N;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.Parser.Parsers
{
    public class PortalParser
    {
        private List<PortalDTO> _listPortals1 = new List<PortalDTO>();
        private readonly List<PortalDTO> ListPortals2 = new List<PortalDTO>();
        private List<MapDTO> _maps;

        public void InsertPortals(List<string[]> packetList)
        {
            _maps = DAOFactory.MapDAO.LoadAll().ToList();
            short map = 0;
            var portalCounter = 0;
            var lodPortal = new PortalDTO
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
            if (DAOFactory.PortalDAO.FirstOrDefault(s => s.SourceMapId == lodPortal.SourceMapId) == null)
            {
                portalCounter++;
                DAOFactory.PortalDAO.InsertOrUpdate(ref lodPortal);
            }

            var minilandPortal = new PortalDTO
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
            if (DAOFactory.PortalDAO.FirstOrDefault(s => s.SourceMapId == minilandPortal.SourceMapId) == null)
            {
                portalCounter++;
                DAOFactory.PortalDAO.InsertOrUpdate(ref minilandPortal);
            }

            var weddingPortal = new PortalDTO
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
            if (DAOFactory.PortalDAO.FirstOrDefault(s => s.SourceMapId == weddingPortal.SourceMapId) == null)
            {
                portalCounter++;
                DAOFactory.PortalDAO.InsertOrUpdate(ref weddingPortal);
            }

            var glacerusCavernPortal = new PortalDTO
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
            if (DAOFactory.PortalDAO.FirstOrDefault(s => s.SourceMapId == glacerusCavernPortal.SourceMapId) == null)
            {
                portalCounter++;
                DAOFactory.PortalDAO.InsertOrUpdate(ref glacerusCavernPortal);
            }

            foreach (var currentPacket in packetList.Where(o => o[0].Equals("at") || o[0].Equals("gp")))
            {
                if (currentPacket.Length > 5 && currentPacket[0] == "at")
                {
                    map = short.Parse(currentPacket[2]);
                    continue;
                }

                if (currentPacket.Length > 4 && currentPacket[0] == "gp")
                {
                    var portal = new PortalDTO
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
                            s.SourceMapId == map && s.SourceX == portal.SourceX && s.SourceY == portal.SourceY
                            && s.DestinationMapId == portal.DestinationMapId)
                        || _maps.All(s => s.MapId != portal.SourceMapId)
                        || _maps.All(s => s.MapId != portal.DestinationMapId))
                    {
                        // Portal already in list
                        continue;
                    }

                    _listPortals1.Add(portal);
                }
            }

            _listPortals1 = _listPortals1.OrderBy(s => s.SourceMapId).ThenBy(s => s.DestinationMapId)
                .ThenBy(s => s.SourceY).ThenBy(s => s.SourceX).ToList();
            foreach (var portal in _listPortals1)
            {
                var p = _listPortals1.Except(ListPortals2).FirstOrDefault(s =>
                    s.SourceMapId == portal.DestinationMapId && s.DestinationMapId == portal.SourceMapId);
                if (p == null)
                {
                    continue;
                }

                portal.DestinationX = p.SourceX;
                portal.DestinationY = p.SourceY;
                p.DestinationY = portal.SourceY;
                p.DestinationX = portal.SourceX;
                ListPortals2.Add(p);
                ListPortals2.Add(portal);
            }

            // foreach portal in the new list of Portals where none (=> !Any()) are found in the existing
            portalCounter = ListPortals2.Count(portal => !DAOFactory.PortalDAO
               .Where(s => s.SourceMapId.Equals(portal.SourceMapId)).Any(
                   s => s.DestinationMapId == portal.DestinationMapId && s.SourceX == portal.SourceX
                       && s.SourceY == portal.SourceY));

            // so this dude doesnt exist yet in DAOFactory -> insert it
            var portalsDtos = ListPortals2.Where(portal => !DAOFactory.PortalDAO
                .Where(s => s.SourceMapId.Equals(portal.SourceMapId)).Any(
                    s => s.DestinationMapId == portal.DestinationMapId && s.SourceX == portal.SourceX
                        && s.SourceY == portal.SourceY));
            DAOFactory.PortalDAO.InsertOrUpdate(portalsDtos);

            Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.PORTALS_PARSED),
                portalCounter));
        }
    }
}