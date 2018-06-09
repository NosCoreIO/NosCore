using NosCore.Shared.Map;
using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.GameObject.Map
{
    public class MapInstancePortalHandler
    {
        #region Properties

        public static int SourceX { get; private set; }

        #endregion

        #region Methods

        public static List<Portal> GenerateMinilandEntryPortals(short entryMap, Guid exitMapinstanceId)
        {
            List<Portal> list = new List<Portal>();

            switch (entryMap)
            {
                case 1:
                    list.Add(new Portal
                    {
                        SourceX = 48,
                        SourceY = 132,
                        DestinationX = 5,
                        DestinationY = 8,
                        Type = (short)PortalType.Miniland,
                        SourceMapId = 1,
                        DestinationMapInstanceId = exitMapinstanceId
                    });
                    break;

                case 145:
                    list.Add(new Portal
                    {
                        SourceX = 9,
                        SourceY = 171,
                        DestinationX = 5,
                        DestinationY = 8,
                        Type = (short)PortalType.Miniland,
                        SourceMapId = 145,
                        DestinationMapInstanceId = exitMapinstanceId
                    });
                    break;
            }

            return list;
        }

        #endregion
    }
}
