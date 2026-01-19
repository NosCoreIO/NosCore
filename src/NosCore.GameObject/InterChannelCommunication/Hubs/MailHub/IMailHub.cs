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

namespace NosCore.GameObject.InterChannelCommunication.Hubs.MailHub;

public interface IMailHub
{
    Task<List<MailData>> GetMails(long id, long characterId, bool senderCopy);
    Task<bool> DeleteMailAsync(long id, long characterId, bool senderCopy);
    Task<MailData?> ViewMailAsync(long id, JsonPatch mailData);
    Task<bool> SendMailAsync(MailRequest mail);
}
