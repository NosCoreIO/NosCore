//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Data.CommandPackets;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.MailHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MailService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Command
{
    public class GiftPacketHandler(IPubSubHub pubSubHub,
            IMailHub mailHttpClient, IClock clock)
        : PacketHandler<GiftPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(GiftPacket giftPacket, ClientSession session)
        {
            var accounts = await pubSubHub.GetSubscribersAsync();
            var receiver = accounts.FirstOrDefault(x => x.ConnectedCharacter?.Name == giftPacket.CharacterName);

            if (receiver == null)
            {
                await session.SendPacketAsync(new InfoiPacket
                {
                    Message = Game18NConstString.UnknownCharacter
                });
                return;
            }

            await mailHttpClient.SendMailAsync(GiftHelper.GenerateMailRequest(clock, session.Character, receiver.ConnectedCharacter!.Id, null, giftPacket.VNum,
                giftPacket.Amount, giftPacket.Rare, giftPacket.Upgrade, false, null, null));
            await session.SendPacketAsync(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = session.Character.CharacterId,
                Type = SayColorType.Red,
                Message = Game18NConstString.GiftDelivered
            });
        }


    }
}
