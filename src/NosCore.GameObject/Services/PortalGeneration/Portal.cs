using System;
using NosCore.Data.StaticEntities;
using NosCore.Packets.ServerPackets;

namespace NosCore.GameObject.Services.PortalGeneration
{
    public class Portal : PortalDTO
    {
        public short MapId { get; set; }
        public Guid DestinationMapInstanceId { get; set; }
        public Guid SourceMapInstanceId { get; set; }

        public GpPacket GenerateGp()
        {
            return new GpPacket
            {
                SourceX = SourceX,
                SourceY = SourceY,
                MapId = MapId,
                PortalType = Type,
                PortalId = PortalId,
                IsDisabled = IsDisabled ? 1 : 0
            };
        }
    }
}