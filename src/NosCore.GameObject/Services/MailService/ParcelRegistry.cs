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

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NosCore.GameObject.InterChannelCommunication.Messages;

namespace NosCore.GameObject.Services.MailService
{
    public class ParcelRegistry : IParcelRegistry
    {
        private readonly ConcurrentDictionary<long, ConcurrentDictionary<bool, ConcurrentDictionary<long, MailData>>> _parcels = new();

        private ConcurrentDictionary<bool, ConcurrentDictionary<long, MailData>> GetOrCreateCharacterParcels(long characterId)
        {
            if (!_parcels.ContainsKey(characterId))
            {
                var characterParcels = new ConcurrentDictionary<bool, ConcurrentDictionary<long, MailData>>();
                characterParcels.TryAdd(false, new ConcurrentDictionary<long, MailData>());
                characterParcels.TryAdd(true, new ConcurrentDictionary<long, MailData>());
                _parcels.TryAdd(characterId, characterParcels);
            }
            return _parcels[characterId];
        }

        public IEnumerable<MailData> GetMails(long characterId)
        {
            var parcels = GetOrCreateCharacterParcels(characterId);
            return parcels[false].Values.Concat(parcels[true].Values);
        }

        public IEnumerable<MailData> GetMails(long characterId, bool senderCopy)
        {
            var parcels = GetOrCreateCharacterParcels(characterId);
            return parcels[senderCopy].Values;
        }

        public MailData? GetMail(long characterId, bool senderCopy, long mailId)
        {
            var parcels = GetOrCreateCharacterParcels(characterId);
            return parcels[senderCopy].TryGetValue(mailId, out var mail) ? mail : null;
        }

        public bool TryAdd(long characterId, bool senderCopy, long mailId, MailData mailData)
        {
            var parcels = GetOrCreateCharacterParcels(characterId);
            return parcels[senderCopy].TryAdd(mailId, mailData);
        }

        public bool TryRemove(long characterId, bool senderCopy, long mailId, out MailData? mailData)
        {
            var parcels = GetOrCreateCharacterParcels(characterId);
            return parcels[senderCopy].TryRemove(mailId, out mailData);
        }

        public void Update(long characterId, bool senderCopy, long mailId, MailData mailData)
        {
            var parcels = GetOrCreateCharacterParcels(characterId);
            parcels[senderCopy][mailId] = mailData;
        }

        public bool ContainsMail(long characterId, bool senderCopy, long mailId)
        {
            var parcels = GetOrCreateCharacterParcels(characterId);
            return parcels[senderCopy].ContainsKey(mailId);
        }

        public long GetMaxMailId(long characterId, bool senderCopy)
        {
            var parcels = GetOrCreateCharacterParcels(characterId);
            return parcels[senderCopy].Select(s => s.Key).DefaultIfEmpty(-1).Max();
        }

        public KeyValuePair<long, MailData>? FindMailByMailDtoId(long characterId, bool senderCopy, long mailDtoId)
        {
            var parcels = GetOrCreateCharacterParcels(characterId);
            var result = parcels[senderCopy].FirstOrDefault(s => s.Value.MailDto.MailId == mailDtoId);
            if (result.Value == null)
            {
                return null;
            }
            return result;
        }
    }
}
