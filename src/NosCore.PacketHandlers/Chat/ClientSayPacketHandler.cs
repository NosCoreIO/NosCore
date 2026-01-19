//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Networking;
using NosCore.Networking.SessionGroup.ChannelMatcher;
using NosCore.Packets.ClientPackets.Chat;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Chat
{
    public class ClientSayPacketHandler : PacketHandler<ClientSayPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(ClientSayPacket clientSayPacket, ClientSession session)
        {
            //TODO: Add a penalty check when it will be ready
            const SayColorType type = SayColorType.Default;
            return session.Character.MapInstance.SendPacketAsync(session.Character.GenerateSay(new SayPacket
            {
                Message = clientSayPacket.Message,
                Type = type
            }), new EveryoneBut(session.Channel!.Id));
        }
    }
}
