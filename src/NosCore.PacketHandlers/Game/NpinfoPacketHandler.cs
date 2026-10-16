//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Player;

namespace NosCore.PacketHandlers.Game
{
    public sealed class NpinfoPacketHandler
        : PacketHandler<NpInfoPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(NpInfoPacket packet, ClientSession session)
        {
            return session.SendPacketsAsync(session.Character.GenerateMateRoster());
        }
    }
}
