//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
// Copyright (C) 2019 - NosCore
//
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;
using NosCore.GameObject.InterChannelCommunication.Messages;

namespace NosCore.GameObject.Services.MailService
{
    public interface IParcelRegistry
    {
        IEnumerable<MailData> GetMails(long characterId);
        IEnumerable<MailData> GetMails(long characterId, bool senderCopy);
        MailData? GetMail(long characterId, bool senderCopy, long mailId);
        bool TryAdd(long characterId, bool senderCopy, long mailId, MailData mailData);
        bool TryRemove(long characterId, bool senderCopy, long mailId, out MailData? mailData);
        void Update(long characterId, bool senderCopy, long mailId, MailData mailData);
        bool ContainsMail(long characterId, bool senderCopy, long mailId);
        long GetMaxMailId(long characterId, bool senderCopy);
        KeyValuePair<long, MailData>? FindMailByMailDtoId(long characterId, bool senderCopy, long mailDtoId);
    }
}
