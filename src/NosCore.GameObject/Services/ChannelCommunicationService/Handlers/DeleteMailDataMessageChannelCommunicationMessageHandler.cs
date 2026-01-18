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
            var player = sessionRegistry.GetPlayer(p => p.VisualId == data.CharacterId);

            if (player == null)
            {
                return;
            }

            var sender = sessionRegistry.GetSenderByCharacterId(player.Value.CharacterId);
            await (sender?.SendPacketAsync(new PostPacket
            {
                Type = 2,
                PostType = data.PostType,
                Id = data.MailId
            }) ?? Task.CompletedTask).ConfigureAwait(false);
        }
    }
}
