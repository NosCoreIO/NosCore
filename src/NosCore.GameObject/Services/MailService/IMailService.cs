//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Json.Patch;
using NosCore.Data.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;
using MailData = NosCore.GameObject.InterChannelCommunication.Messages.MailData;

namespace NosCore.GameObject.Services.MailService
{
    public interface IMailService
    {
        List<MailData> GetMails(long id, long characterId, bool senderCopy);

        Task<bool> DeleteMailAsync(long id, long characterId, bool senderCopy);

        Task<MailData?> EditMailAsync(long id, JsonPatch mailData);

        Task<bool> SendMailAsync(MailDto mail, short? vNum, short? amount, sbyte? rare, byte? upgrade);
    }
}
