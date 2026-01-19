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

using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.GameObject.InterChannelCommunication.Messages;

namespace NosCore.GameObject.Services.MailService
{
    public class ParcelRegistry : IParcelRegistry
    {
        private readonly ConcurrentDictionary<long, ConcurrentDictionary<bool, ConcurrentDictionary<long, MailData>>> _parcels = new();
        private readonly IDao<CharacterDto, long> _characterDao;
        private readonly IDao<IItemInstanceDto?, Guid> _itemInstanceDao;
        private readonly List<ItemDto> _items;
        private readonly IDao<MailDto, long> _mailDao;

        public ParcelRegistry(IDao<CharacterDto, long> characterDao, IDao<MailDto, long> mailDao, List<ItemDto> items,
            IDao<IItemInstanceDto?, Guid> itemInstanceDao)
        {
            _mailDao = mailDao;
            _items = items;
            _characterDao = characterDao;
            _itemInstanceDao = itemInstanceDao;
            _ = InitializeAsync();
        }

        public ConcurrentDictionary<long, MailData> GetMails(long characterId, bool isSenderCopy)
        {
            EnsureCharacterExists(characterId);
            return _parcels[characterId][isSenderCopy];
        }

        public MailData? GetMail(long characterId, bool isSenderCopy, long mailId)
        {
            EnsureCharacterExists(characterId);
            return _parcels[characterId][isSenderCopy].TryGetValue(mailId, out var mail) ? mail : null;
        }

        public void AddMail(long characterId, bool isSenderCopy, long mailId, MailData mailData)
        {
            EnsureCharacterExists(characterId);
            _parcels[characterId][isSenderCopy].TryAdd(mailId, mailData);
        }

        public bool RemoveMail(long characterId, bool isSenderCopy, long mailId, out MailData? mailData)
        {
            EnsureCharacterExists(characterId);
            return _parcels[characterId][isSenderCopy].TryRemove(mailId, out mailData);
        }

        public void UpdateMail(long characterId, bool isSenderCopy, long mailId, MailData mailData)
        {
            EnsureCharacterExists(characterId);
            _parcels[characterId][isSenderCopy][mailId] = mailData;
        }

        public long GetNextMailId(long characterId, bool isSenderCopy)
        {
            EnsureCharacterExists(characterId);
            return _parcels[characterId][isSenderCopy].Select(s => s.Key).DefaultIfEmpty(-1).Max() + 1;
        }

        private void EnsureCharacterExists(long characterId)
        {
            if (_parcels.ContainsKey(characterId))
            {
                return;
            }

            _parcels.TryAdd(characterId, new ConcurrentDictionary<bool, ConcurrentDictionary<long, MailData>>());
            _parcels[characterId].TryAdd(false, new ConcurrentDictionary<long, MailData>());
            _parcels[characterId].TryAdd(true, new ConcurrentDictionary<long, MailData>());
        }

        private async Task InitializeAsync()
        {
            var mails = _mailDao.LoadAll().ToList();
            var idcopy = 0;
            var idmail = 0;
            var charactersIds = mails.Select(s => s.ReceiverId)
                .Union(mails.Where(s => s.SenderId != null).Select(s => (long)s.SenderId!));
            var characternames = new Dictionary<long, string?>();
            foreach (var characterId in charactersIds)
            {
                characternames.Add(characterId, (await _characterDao.FirstOrDefaultAsync(s => s.CharacterId == characterId))?.Name);
            }

            foreach (var mail in mails)
            {
                var itinst = await _itemInstanceDao.FirstOrDefaultAsync(s => s!.Id == mail.ItemInstanceId);
                ItemDto? it = null;
                if (itinst != null)
                {
                    it = _items.FirstOrDefault(s => s.VNum == itinst.ItemVNum);
                }

                var senderName = mail.SenderId == null ? "NOSMALL" : characternames[(long)mail.SenderId];
                var receiverName = characternames[mail.ReceiverId];
                var mailId = mail.IsSenderCopy ? (short)idcopy : (short)idmail;
                var targetCharacterId = mail.IsSenderCopy ? mail.SenderId ?? 0 : mail.ReceiverId;

                AddMail(targetCharacterId, mail.IsSenderCopy, mailId,
                    new MailData
                    {
                        ItemInstance = (ItemInstanceDto?)itinst,
                        SenderName = senderName,
                        ReceiverName = receiverName,
                        MailId = mailId,
                        MailDto = mail,
                        ItemType = (short?)it?.ItemType ?? -1
                    });

                if (mail.IsSenderCopy)
                {
                    idcopy++;
                }
                else
                {
                    idmail++;
                }
            }
        }
    }
}
