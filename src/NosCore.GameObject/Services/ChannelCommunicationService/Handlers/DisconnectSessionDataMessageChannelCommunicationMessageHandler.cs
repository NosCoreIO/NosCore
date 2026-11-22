//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Shared.I18N;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ChannelCommunicationService.Handlers
{
    public class DisconnectSessionDataMessageChannelCommunicationMessageHandler(ISessionRegistry sessionRegistry,
            ILogger<DisconnectSessionDataMessageChannelCommunicationMessageHandler> logger,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        : ChannelCommunicationMessageHandler<DisconnectSessionData>
    {
        public override async Task Handle(DisconnectSessionData data)
        {
            var session = sessionRegistry.GetAllSessions()
                .FirstOrDefault(s => s.SessionId == data.SessionId && s.AccountName == data.AccountName);
            // A session handed over to another channel no longer holds the account, and closing it
            // early would cut the client off before it is told where to go.
            if (session == null || session.Sender is ClientSession { IsHandedOver: true })
            {
                return;
            }

            logger.LogInformation(logLanguage[LogLanguageKey.SESSION_TAKEN_OVER], data.AccountName, data.SessionId);
            await session.Disconnect();
        }
    }
}
