//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets.Specialists;

namespace NosCore.PacketHandlers.Game
{
    // Post-OpenNos "refresh pet tab" request; the `_cts` suffix is the client-to-server
    // convention (same as frank_cts / fhis_cts). OpenNos never implemented a handler
    // because the packet post-dates the codebase. Siblings of this packet (in a live
    // session) also come with sc_p_stc 0 0 from the server to confirm the roster cap.
    // Mate subsystem isn't wired, so we answer with sc_p_stc 0 (empty roster) and
    // leave it at that.
    public sealed class ScpCtsPacketHandler
        : PacketHandler<ScpCtsPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(ScpCtsPacket packet, ClientSession session)
        {
            return session.SendPacketAsync(new ScPStcPacket { MaxMateCountTenths = 0 });
        }
    }
}
