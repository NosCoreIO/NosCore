//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Json.Patch;
using Microsoft.Extensions.DependencyInjection;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Services.MailService;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.MailHub
{
    public class MailHub(IMailService mailService) : IMailHub
    {
        public Task<List<MailData>> GetMails(long id, long characterId, bool senderCopy) => Task.FromResult(mailService.GetMails(id, characterId, senderCopy));

        public Task<bool> DeleteMailAsync(long id, long characterId, bool senderCopy) => mailService.DeleteMailAsync(id, characterId, senderCopy);

        public Task<MailData?> ViewMailAsync(long id, JsonPatch mailData) => mailService.EditMailAsync(id, mailData);

        public Task<bool> SendMailAsync(MailRequest mail) => mailService.SendMailAsync(mail.Mail!, mail.VNum, mail.Amount, mail.Rare, mail.Upgrade);

        public static async Task<Reply<List<MailData>>> Handle(GetMailsRequest r, IServiceProvider services) => new(await services.GetRequiredService<MailHub>().GetMails(r.Id, r.CharacterId, r.SenderCopy));

        public static async Task<Reply<bool>> Handle(DeleteMailRequest r, IServiceProvider services) => new(await services.GetRequiredService<MailHub>().DeleteMailAsync(r.Id, r.CharacterId, r.SenderCopy));

        public static async Task<Reply<MailData?>> Handle(ViewMailRequest r, IServiceProvider services) => new(await services.GetRequiredService<MailHub>().ViewMailAsync(r.Id, r.MailData));

        public static async Task<Reply<bool>> Handle(SendMailRequest r, IServiceProvider services) => new(await services.GetRequiredService<MailHub>().SendMailAsync(r.Mail));
    }
}
