//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Networking;
using NosCore.Packets.ClientPackets.Chat;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using System.Threading.Tasks;


namespace NosCore.PacketHandlers.Group
{
    public class GroupTalkPacketHandler : PacketHandler<GroupTalkPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(GroupTalkPacket groupTalkPacket, ClientSession clientSession)
        {
            if (clientSession.Character.Group!.Count == 1)
            {
                return Task.CompletedTask;
            }

            return clientSession.Character.Group.SendPacketAsync(
                clientSession.Character.GenerateSpk(new SpeakPacket
                { Message = groupTalkPacket.Message, SpeakType = SpeakType.Group }));
        }
    }
}
