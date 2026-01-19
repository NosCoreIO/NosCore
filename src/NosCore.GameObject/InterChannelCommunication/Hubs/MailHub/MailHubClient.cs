//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Json.Patch;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Messages;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.MailHub
{
    public class MailHubClient(HubConnectionFactory hubConnectionFactory, ILogger logger)
        : BaseHubClient(hubConnectionFactory, nameof(MailHub), logger), IMailHub
    {
        public Task<List<MailData>> GetMails(long id, long characterId, bool senderCopy) =>
            InvokeAsync<List<MailData>>(nameof(GetMails), id, characterId, senderCopy);

        public Task<bool> DeleteMailAsync(long id, long characterId, bool senderCopy) =>
            InvokeAsync<bool>(nameof(DeleteMailAsync), id, characterId, senderCopy);

        public Task<MailData?> ViewMailAsync(long id, JsonPatch mailData) =>
            InvokeAsync<MailData?>(nameof(ViewMailAsync), id, mailData);

        public Task<bool> SendMailAsync(MailRequest mail) =>
            InvokeAsync<bool>(nameof(SendMailAsync), mail);
    }
}
