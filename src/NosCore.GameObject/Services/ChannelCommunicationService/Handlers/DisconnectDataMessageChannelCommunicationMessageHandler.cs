using NosCore.GameObject.Services.BroadcastService;
using System.Threading.Tasks;
using DisconnectData = NosCore.GameObject.InterChannelCommunication.Messages.DisconnectData;

namespace NosCore.GameObject.Services.ChannelCommunicationService.Handlers
{
    public class DisconnectDataMessageChannelCommunicationMessageHandler(ISessionRegistry sessionRegistry) : ChannelCommunicationMessageHandler<DisconnectData>
    {
        public override async Task Handle(DisconnectData data)
        {
            var targetCharacter = sessionRegistry.GetCharacter(s => s.VisualId == data.CharacterId);
            if (targetCharacter == null)
            {
                return;
            }

            await sessionRegistry.DisconnectByCharacterIdAsync(data.CharacterId);
        }
    }
}
