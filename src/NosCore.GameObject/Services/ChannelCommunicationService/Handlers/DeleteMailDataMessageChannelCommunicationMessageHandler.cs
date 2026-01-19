using System.Threading.Tasks;
using NosCore.Packets.ServerPackets.Parcel;
using NosCore.GameObject.Services.BroadcastService;
using DeleteMailData = NosCore.GameObject.InterChannelCommunication.Messages.DeleteMailData;

namespace NosCore.GameObject.Services.ChannelCommunicationService.Handlers
{
    public class DeleteMailDataMessageChannelCommunicationMessageHandler(ISessionRegistry sessionRegistry) : ChannelCommunicationMessageHandler<DeleteMailData>
    {
        public override async Task Handle(DeleteMailData data)
        {
            var session = sessionRegistry.GetCharacter(s => s.VisualId == data.CharacterId);

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
