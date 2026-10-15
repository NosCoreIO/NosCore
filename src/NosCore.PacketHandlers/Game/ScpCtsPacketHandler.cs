//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets;

namespace NosCore.PacketHandlers.Game
{
    // The official server leaves the pet window's refresh request unanswered.
    public sealed class ScpCtsPacketHandler
        : PacketHandler<ScpCtsPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(ScpCtsPacket packet, ClientSession session) => Task.CompletedTask;
    }
}
