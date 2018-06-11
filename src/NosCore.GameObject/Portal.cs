using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking;
using NosCore.Packets.ServerPackets;
using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.GameObject
{
    public class Portal : PortalDTO
    {
        #region Instantiation

        public Portal()
        { }

        #endregion
        
        public GpPacket GenerateGp()
        {
            return new GpPacket()
            {
                SourceX = SourceX,
                SourceY = SourceY,
                MapId = ServerManager.Instance.GetMapInstance(DestinationMapInstanceId)?.Map.MapId ?? 0,
                PortalType = Type,
                PortalId = PortalId,
                IsDisabled = IsDisabled ? 1 : 0
            };
        }

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
                    _destinationMapInstanceId = ServerManager.Instance.GetBaseMapInstanceIdByMapId(DestinationMapId);
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
                    _sourceMapInstanceId = ServerManager.Instance.GetBaseMapInstanceIdByMapId(SourceMapId);
                }

                return _sourceMapInstanceId;
            }
            set => _sourceMapInstanceId = value;
        }

        #endregion
    }
}
