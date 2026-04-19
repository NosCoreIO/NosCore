//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.BroadcastService;
using System.Threading.Tasks;
using DisconnectData = NosCore.GameObject.InterChannelCommunication.Messages.DisconnectData;

namespace NosCore.GameObject.Services.ChannelCommunicationService.Handlers
{
    public class DisconnectDataMessageChannelCommunicationMessageHandler(ISessionRegistry sessionRegistry) : ChannelCommunicationMessageHandler<DisconnectData>
    {
        public override async Task Handle(DisconnectData data)
        {
            var session = sessionRegistry.GetSessionByCharacterId(data.CharacterId);
            if (session == null)
            {
                return;
            }

            await sessionRegistry.DisconnectByCharacterIdAsync(data.CharacterId);
        }
    }
}
