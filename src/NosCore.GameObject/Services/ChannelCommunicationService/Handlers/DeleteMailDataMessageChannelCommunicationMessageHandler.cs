using System.Threading.Tasks;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Networking;
using NosCore.Packets.ServerPackets.Parcel;
using DeleteMailData = NosCore.GameObject.InterChannelCommunication.Messages.DeleteMailData;

namespace NosCore.GameObject.Services.ChannelCommunicationService.Handlers
{
    public class DeleteMailDataMessageChannelCommunicationMessageHandler(IPubSubHub pubSubHub) : ChannelCommunicationMessageHandler<DeleteMailData>
    {
        public override async Task Handle(DeleteMailData data)
        {
            var session = Broadcaster.Instance.GetCharacter(s => s.VisualId == data.CharacterId);

            if (session == null)
            {
                return;
            }

            await session.SendPacketAsync(new PostPacket
            {
                Type = 2,
                PostType = data.PostType,
                Id = data.MailId
            }).ConfigureAwait(false);

            await pubSubHub.DeleteMessageAsync(data.Id);
        }
    }
}
