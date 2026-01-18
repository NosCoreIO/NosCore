using System.Threading.Tasks;
using NosCore.GameObject.Services.BroadcastService;
using DisconnectData = NosCore.GameObject.InterChannelCommunication.Messages.DisconnectData;

namespace NosCore.GameObject.Services.ChannelCommunicationService.Handlers
{
    public class DisconnectDataMessageChannelCommunicationMessageHandler(ISessionRegistry sessionRegistry) : ChannelCommunicationMessageHandler<DisconnectData>
    {
        public override async Task Handle(DisconnectData data)
        {
            var player = sessionRegistry.GetPlayer(p => p.VisualId == data.CharacterId);
            if (player == null)
            {
                return;
            }

            await sessionRegistry.DisconnectByCharacterIdAsync(data.CharacterId).ConfigureAwait(false);
        }
    }
}
