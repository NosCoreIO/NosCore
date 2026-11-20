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
            var owner = data.MailDto.IsSenderCopy ? data.SenderName : data.ReceiverName;
            var session = sessionRegistry.GetSession(s => s.Character.Name == owner);

            if (session == null)
            {
                return;
            }

            await session.GenerateMailAsync(new[] { data });
            if (data.ItemInstance != null && !data.MailDto.IsSenderCopy)
            {
                await session.SendPacketAsync(session.Character.GenerateSayi(SayColorType.Yellow, Game18NConstString.NewParcelArrived2));
            }
        }
    }
}
