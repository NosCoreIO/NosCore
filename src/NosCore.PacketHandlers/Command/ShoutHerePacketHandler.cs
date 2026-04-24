//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.Data.CommandPackets;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Networking;
using NosCore.Packets.Enumerations;

namespace NosCore.PacketHandlers.Command
{
    public class ShoutHerePacketHandler : PacketHandler<ShoutHerePacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(ShoutHerePacket packet, ClientSession session)
        {
            if (string.IsNullOrWhiteSpace(packet.Message))
            {
                return session.SendPacketAsync(session.Character.GenerateSay(packet.Help(), SayColorType.Yellow));
            }

            return session.Character.MapInstance.SendPacketAsync(
                session.Character.GenerateSay(packet.Message, SayColorType.Yellow));
        }
    }
}
