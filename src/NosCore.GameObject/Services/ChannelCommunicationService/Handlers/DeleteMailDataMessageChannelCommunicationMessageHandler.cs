//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.ServerPackets.Parcel;
using System.Threading.Tasks;
using DeleteMailData = NosCore.GameObject.InterChannelCommunication.Messages.DeleteMailData;

namespace NosCore.GameObject.Services.ChannelCommunicationService.Handlers
{
    public class DeleteMailDataMessageChannelCommunicationMessageHandler(ISessionRegistry sessionRegistry) : ChannelCommunicationMessageHandler<DeleteMailData>
    {
        public override async Task Handle(DeleteMailData data)
        {
            var session = sessionRegistry.GetSession(s => s.Character.VisualId == data.CharacterId);

            if (session == null)
            {
                return;
            }

            await session.SendPacketAsync(new PostPacket
            {
                Type = 2,
                PostType = data.PostType,
                Id = data.MailId
            });
        }
    }
}
