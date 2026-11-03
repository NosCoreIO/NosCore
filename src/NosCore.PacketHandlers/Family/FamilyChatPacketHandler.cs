//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Chat;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Family
{
    public class FamilyChatPacketHandler(IPubSubHub pubSubHub) : PacketHandler<FamilyChatPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(FamilyChatPacket familyChatPacket, ClientSession session)
        {
            var family = session.Character.Family;
            if (family == null || string.IsNullOrWhiteSpace(familyChatPacket.Message))
            {
                return;
            }

            await pubSubHub.SendMessageAsync(new FamilyChatData
            {
                FamilyId = family.FamilyId,
                SenderId = session.Character.VisualId,
                SenderName = session.Character.Name,
                SenderMapInstanceId = session.Character.MapInstanceId,
                Message = familyChatPacket.Message.Trim()
            });
        }
    }
}
