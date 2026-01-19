//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Json.Patch;
using Microsoft.AspNetCore.SignalR;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Services.MailService;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.MailHub
{
    public class MailHub(IMailService mailService) : Hub, IMailHub
    {
        public Task<List<MailData>> GetMails(long id, long characterId, bool senderCopy) => Task.FromResult(mailService.GetMails(id, characterId, senderCopy));

        public Task<bool> DeleteMailAsync(long id, long characterId, bool senderCopy) => mailService.DeleteMailAsync(id, characterId, senderCopy);

        public Task<MailData?> ViewMailAsync(long id, JsonPatch mailData) => mailService.EditMailAsync(id, mailData);

        public Task<bool> SendMailAsync(MailRequest mail) => mailService.SendMailAsync(mail.Mail!, mail.VNum, mail.Amount, mail.Rare, mail.Upgrade);
    }
}
