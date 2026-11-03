//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;
using System.Threading.Tasks;
using MailData = NosCore.GameObject.InterChannelCommunication.Messages.MailData;

namespace NosCore.GameObject.Services.ChannelCommunicationService.Handlers
{
    public class MailDataMessageChannelCommunicationMessageHandler(ISessionRegistry sessionRegistry) : ChannelCommunicationMessageHandler<MailData>
    {
        public override async Task Handle(MailData data)
        {
            var session = sessionRegistry.GetSession(s => s.Character.Name == data.ReceiverName);

            if (session == null)
            {
                return;
            }

            var character = session.Character;
            if (data.ItemInstance != null)
            {
                await session.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = character.CharacterId,
                    Type = SayColorType.Green,
                    Message = Game18NConstString.ParcelReceived,
                    ArgumentType = 2,
                    Game18NArguments = { data.ItemInstance.ItemVNum.ToString(), data.ItemInstance.Amount }
                });
            }

            await session.GenerateMailAsync(new[] { data });
        }
    }
}
