using System.Threading.Tasks;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Networking;
using DisconnectData = NosCore.GameObject.InterChannelCommunication.Messages.DisconnectData;


namespace NosCore.GameObject.Services.ChannelCommunicationService.Handlers
{
    public class DisconnectDataMessageChannelCommunicationMessageHandler(IPubSubHub pubSubHub) : ChannelCommunicationMessageHandler<DisconnectData>
    {
        public override async Task Handle(DisconnectData data)
        {
            var targetSession = Broadcaster.Instance.GetCharacter(s => s.VisualId == data.CharacterId) as Character;
            if (targetSession?.Session == null)
            {
                return;
            }

            await targetSession.Session.DisconnectAsync().ConfigureAwait(false);
            await pubSubHub.DeleteMessageAsync(data.Id);
        }
    }
}
