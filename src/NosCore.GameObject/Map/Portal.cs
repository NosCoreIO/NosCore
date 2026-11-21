//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.StaticEntities;
using NosCore.Packets.ServerPackets.Portals;
using System;

namespace NosCore.GameObject.Map
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

        // The client draws a portal again on each gp line without taking the previous drawing down, so a portal that
        // changes state is taken off first.
        public GpPacket GenerateRemovalGp()
        {
            var gp = GenerateGp();
            gp.IsDisabled = true;
            return gp;
        }
    }
}
