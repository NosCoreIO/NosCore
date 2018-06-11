using NosCore.DAL;
using NosCore.Data.StaticEntities;
using NosCore.Shared.Logger;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.Parser.Parsers
{
    public class PortalParser
    {
        private static List<PortalDTO> listPortals1 = new List<PortalDTO>();
        private static List<PortalDTO> listPortals2 = new List<PortalDTO>();
        private List<MapDTO> maps = DAOFactory.MapDAO.LoadAll().ToList();

        public void InsertPortals(List<string[]> packetList)
        {
            short map = 0;

            var lodPortal = new PortalDTO
            {
                SourceMapId = 150,
                SourceX = 172,
                SourceY = 171,
                DestinationMapId = 98,
                Type = Shared.Map.PortalType.MapPortal,
                DestinationX = 6,
                DestinationY = 36,
                IsDisabled = false
            };
            DAOFactory.PortalDAO.InsertOrUpdate(ref lodPortal);

            var minilandPortal = new PortalDTO
            {
                SourceMapId = 20001,
                SourceX = 3,
                SourceY = 8,
                DestinationMapId = 1,
                Type = Shared.Map.PortalType.MapPortal,
                DestinationX = 48,
                DestinationY = 132,
                IsDisabled = false
            };
            DAOFactory.PortalDAO.InsertOrUpdate(ref minilandPortal);

            var weddingPortal = new PortalDTO
            {
                SourceMapId = 2586,
                SourceX = 34,
                SourceY = 54,
                DestinationMapId = 145,
                Type = Shared.Map.PortalType.MapPortal,
                DestinationX = 61,
                DestinationY = 165,
                IsDisabled = false
            };
            DAOFactory.PortalDAO.InsertOrUpdate(ref weddingPortal);

            var glacerusCavernPortal = new PortalDTO
            {
                SourceMapId = 2587,
                SourceX = 42,
                SourceY = 3,
                DestinationMapId = 189,
                Type = Shared.Map.PortalType.MapPortal,
                DestinationX = 48,
                DestinationY = 156,
                IsDisabled = false
            };
            DAOFactory.PortalDAO.InsertOrUpdate(ref glacerusCavernPortal);

            foreach (string[] currentPacket in packetList.Where(o => o[0].Equals("at") || o[0].Equals("gp")))
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
                        Type = (Shared.Map.PortalType)Enum.Parse(typeof(Shared.Map.PortalType), currentPacket[4]),
                        DestinationX = -1,
                        DestinationY = -1,
                        IsDisabled = false
                    };

                    if (listPortals1.Any(s => s.SourceMapId == map && s.SourceX == portal.SourceX && s.SourceY == portal.SourceY && s.DestinationMapId == portal.DestinationMapId) || maps.All(s => s.MapId != portal.SourceMapId) || maps.All(s => s.MapId != portal.DestinationMapId))
                    {
                        // Portal already in list
                        continue;
                    }

                    listPortals1.Add(portal);
                }
            }

            listPortals1 = listPortals1.OrderBy(s => s.SourceMapId).ThenBy(s => s.DestinationMapId).ThenBy(s => s.SourceY).ThenBy(s => s.SourceX).ToList();
            foreach (PortalDTO portal in listPortals1)
            {
                var p = listPortals1.Except(listPortals2).FirstOrDefault(s => s.SourceMapId == portal.DestinationMapId && s.DestinationMapId == portal.SourceMapId);
                if (p == null)
                {
                    continue;
                }

                portal.DestinationX = p.SourceX;
                portal.DestinationY = p.SourceY;
                p.DestinationY = portal.SourceY;
                p.DestinationX = portal.SourceX;
                listPortals2.Add(p);
                listPortals2.Add(portal);
            }

            // foreach portal in the new list of Portals where none (=> !Any()) are found in the existing
            var portalCounter = listPortals2.Count(portal => !DAOFactory.PortalDAO.Where(s => s.SourceMapId.Equals(portal.SourceMapId)).Any(
                s => s.DestinationMapId == portal.DestinationMapId && s.SourceX == portal.SourceX && s.SourceY == portal.SourceY));

            // so this dude doesnt exist yet in DAOFactory -> insert it
            IEnumerable<PortalDTO> portalsDtos = listPortals2.Where(portal => !DAOFactory.PortalDAO.Where(s => s.SourceMapId.Equals(portal.SourceMapId)).Any(
                s => s.DestinationMapId == portal.DestinationMapId && s.SourceX == portal.SourceX && s.SourceY == portal.SourceY));
            DAOFactory.PortalDAO.InsertOrUpdate(portalsDtos);

            Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.PORTALS_PARSED), portalCounter));
        }
    }
}
