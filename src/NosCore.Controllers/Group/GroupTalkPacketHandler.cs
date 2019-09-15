using ChickenAPI.Packets.ClientPackets.Chat;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.Chats;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;

namespace NosCore.PacketHandlers.Group
{
    public class GroupTalkPacketHandler : PacketHandler<GroupTalkPacket>, IWorldPacketHandler
    {
        public override void Execute(GroupTalkPacket groupTalkPacket, ClientSession clientSession)
        {
            if (clientSession.Character.Group.Count == 1)
            {
                return;
            }

            clientSession.Character.Group.Sessions.SendPacket(
                clientSession.Character.GenerateSpk(new SpeakPacket
                { Message = groupTalkPacket.Message, SpeakType = SpeakType.Group }));
        }
    }
}
