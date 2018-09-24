using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking;
using NosCore.Packets.ServerPackets;

namespace NosCore.GameObject
{
    public class Portal : PortalDTO
    {
        public GpPacket GenerateGp()
        {
            return new GpPacket
            {
                SourceX = SourceX,
                SourceY = SourceY,
                MapId = _mapInstances.GetMapInstance(DestinationMapInstanceId)?.Map.MapId ?? 0,
                PortalType = Type,
                PortalId = PortalId,
                IsDisabled = IsDisabled ? 1 : 0
            };
        }

        #region Instantiation

        #endregion

        #region Members

        private Guid _destinationMapInstanceId;
        private Guid _sourceMapInstanceId;

        #endregion

        #region Properties

        public Guid DestinationMapInstanceId
        {
            get
            {
                if (_destinationMapInstanceId == default(Guid) && DestinationMapId != -1)
                {
                    _destinationMapInstanceId = _mapInstances.GetBaseMapInstanceIdByMapId(DestinationMapId);
                }

                return _destinationMapInstanceId;
            }
            set => _destinationMapInstanceId = value;
        }

        public Guid SourceMapInstanceId
        {
            get
            {
                if (_sourceMapInstanceId == default(Guid))
                {
                    _sourceMapInstanceId = _mapInstances.GetBaseMapInstanceIdByMapId(SourceMapId);
                }

                return _sourceMapInstanceId;
            }
            set => _sourceMapInstanceId = value;
        }

        #endregion
    }
}