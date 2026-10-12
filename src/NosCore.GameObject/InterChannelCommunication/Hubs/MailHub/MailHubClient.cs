//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Json.Patch;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Messages;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wolverine;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.MailHub
{
    public class MailHubClient(IMessageBus bus) : IMailHub
    {
        public Task<List<MailData>> GetMails(long id, long characterId, bool senderCopy) =>
            bus.RequestAsync<List<MailData>>(new GetMailsRequest(id, characterId, senderCopy));

        public Task<bool> DeleteMailAsync(long id, long characterId, bool senderCopy) =>
            bus.RequestAsync<bool>(new DeleteMailRequest(id, characterId, senderCopy));

        public Task<MailData?> ViewMailAsync(long id, JsonPatch mailData) =>
            bus.RequestAsync<MailData?>(new ViewMailRequest(id, mailData));

        public Task<bool> SendMailAsync(MailRequest mail) => bus.RequestAsync<bool>(new SendMailRequest(mail));
    }
}
