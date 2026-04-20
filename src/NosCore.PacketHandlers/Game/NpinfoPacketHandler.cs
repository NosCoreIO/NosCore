//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.PacketHandlers.Game
{
    // Client sends `npinfo <page>` when it opens or pages through the pet panel.
    // OpenNos BasicPacketHandler.GetStats replies with stat_char, caches the page,
    // emits p_clear, then a sc_p per Mate on that page.
    //
    // NosCore has no runtime Mate subsystem yet, so we reply with p_clear so the
    // client renders an empty roster rather than the "loading" state that pins the
    // tab open forever. No sc_p follow-ups.
    public sealed class NpinfoPacketHandler
        : PacketHandler<NpInfoPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(NpInfoPacket packet, ClientSession session)
        {
            return session.SendPacketAsync(new PclearPacket());
        }
    }
}
