//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.InterChannelCommunication.Messages;
using System.Collections.Concurrent;

namespace NosCore.GameObject.Services.MailService
{
    public interface IParcelRegistry
    {
        ConcurrentDictionary<long, MailData> GetMails(long characterId, bool isSenderCopy);
        MailData? GetMail(long characterId, bool isSenderCopy, long mailId);
        void AddMail(long characterId, bool isSenderCopy, long mailId, MailData mailData);
        bool RemoveMail(long characterId, bool isSenderCopy, long mailId, out MailData? mailData);
        void UpdateMail(long characterId, bool isSenderCopy, long mailId, MailData mailData);
        long GetNextMailId(long characterId, bool isSenderCopy);
    }
}
