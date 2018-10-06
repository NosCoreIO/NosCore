using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Services.MapInstanceAccess;
using NosCore.Packets.ServerPackets;

namespace NosCore.GameObject
{
    public class Portal : PortalDTO
    {
        private readonly MapInstanceAccessService _mapInstanceAccessService;

        public Portal(MapInstanceAccessService mapInstanceAccessService)
        {
            _mapInstanceAccessService = mapInstanceAccessService;
        }

        public GpPacket GenerateGp()
        {
            return new GpPacket
            {
                SourceX = SourceX,
                SourceY = SourceY,
                MapId = _mapInstanceAccessService.GetMapInstance(DestinationMapInstanceId)?.Map.MapId ?? 0,
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
                    _destinationMapInstanceId = _mapInstanceAccessService.GetBaseMapInstanceIdByMapId(DestinationMapId);
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
                    _sourceMapInstanceId = _mapInstanceAccessService.GetBaseMapInstanceIdByMapId(SourceMapId);
                }

                return _sourceMapInstanceId;
            }
            set => _sourceMapInstanceId = value;
        }

        #endregion
    }
}