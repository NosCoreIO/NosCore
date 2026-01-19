//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Movement;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Game
{
    public class PulsePacketHandler : PacketHandler<PulsePacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(PulsePacket pulsePacket, ClientSession session)
        {
            session.LastPulse += 60;
            if (pulsePacket.Tick != session.LastPulse)
            {
                await session.DisconnectAsync();
            }
        }
    }
}
