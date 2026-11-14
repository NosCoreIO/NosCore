//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.CommandPackets;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Commands;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Friend
{
    public class FlCommandPacketHandler : PacketHandler<FlCommandPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(FlCommandPacket flPacket, ClientSession session) =>
            session.HandlePacketsAsync(new[] { new FriendInvitePacket { CharacterName = flPacket.CharacterName ?? string.Empty } });
    }
}
