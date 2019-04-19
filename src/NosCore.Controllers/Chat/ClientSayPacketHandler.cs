using ChickenAPI.Packets.ClientPackets.Chat;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.Chats;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ChannelMatcher;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;

namespace NosCore.PacketHandlers.Chat
{
    public class ClientSayPacketHandler : PacketHandler<ClientSayPacket>, IWorldPacketHandler
    {
        public override void Execute(ClientSayPacket clientSayPacket, ClientSession session)
        {

            //TODO: Add a penalty check when it will be ready
            const SayColorType type = SayColorType.White;
            session.Character.MapInstance?.Sessions.SendPacket(session.Character.GenerateSay(new SayPacket
            {
                Message = clientSayPacket.Message,
                Type = type
            }), new EveryoneBut(session.Channel.Id)); //TODO  ReceiverType.AllExceptMeAndBlacklisted
        }
    }
}
