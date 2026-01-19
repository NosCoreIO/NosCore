//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.StaticEntities;
using NosCore.Packets.ServerPackets.Portals;
using System;

namespace NosCore.GameObject.ComponentEntities.Entities
{
    public class Portal : PortalDto
    {
        public Guid DestinationMapInstanceId { get; set; }
        public Guid SourceMapInstanceId { get; set; }

        public GpPacket GenerateGp()
        {
            return new GpPacket
            {
                SourceX = SourceX,
                SourceY = SourceY,
                MapId = DestinationMapId,
                PortalType = Type,
                PortalId = PortalId,
                IsDisabled = IsDisabled
            };
        }
    }
}
