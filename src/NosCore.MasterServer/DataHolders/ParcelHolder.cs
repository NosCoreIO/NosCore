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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mapster;
using NosCore.Core;
using NosCore.Dao.Interfaces;
using NosCore.Data;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;

namespace NosCore.MasterServer.DataHolders
{
    public class ParcelHolder : ConcurrentDictionary<long,
        ConcurrentDictionary<bool, ConcurrentDictionary<long, MailData>>>
    {
        private readonly IDao<CharacterDto, long> _characterDao;
        private readonly IDao<IItemInstanceDto, Guid> _itemInstanceDao;
        private readonly List<ItemDto> _items;
        private readonly IDao<MailDto, long> _mailDao;

        public ParcelHolder(IDao<CharacterDto, long> characterDao, IDao<MailDto, long> mailDao, List<ItemDto> items,
            IDao<IItemInstanceDto, Guid> itemInstanceDao)
        {
            _mailDao = mailDao;
            _items = items;
            _characterDao = characterDao;
            _itemInstanceDao = itemInstanceDao;
            InitializeAsync();
        }

        public new ConcurrentDictionary<bool, ConcurrentDictionary<long, MailData>> this[long characterId]
        {
            // returns value if exists
            get
            {
                if (ContainsKey(characterId))
                {
                    return this.First(s => s.Key == characterId).Value;
                }

                TryAdd(characterId, new ConcurrentDictionary<bool, ConcurrentDictionary<long, MailData>>());
                this.First(s => s.Key == characterId).Value
                    .TryAdd(false, new ConcurrentDictionary<long, MailData>());
                this.First(s => s.Key == characterId).Value
                    .TryAdd(true, new ConcurrentDictionary<long, MailData>());

                return this.First(s => s.Key == characterId).Value;
            }
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
                characternames.Add(characterId, (await _characterDao.FirstOrDefaultAsync(s => s.CharacterId == characterId).ConfigureAwait(false))?.Name);
            }

            foreach (var mail in mails)
            {
                var itinst = await _itemInstanceDao.FirstOrDefaultAsync(s => s.Id == mail.ItemInstanceId).ConfigureAwait(false);
                ItemDto? it = null;
                if (itinst != null)
                {
                    it = _items.FirstOrDefault(s => s.VNum == itinst.ItemVNum);
                }

                var senderName = mail.SenderId == null ? "NOSMALL" : characternames[(long)mail.SenderId];
                var receiverName = characternames[mail.ReceiverId];
                var mailId = mail.IsSenderCopy ? (short)idcopy : (short)idmail;
                this[mail.IsSenderCopy ? mail.SenderId ?? 0 : mail.ReceiverId][mail.IsSenderCopy].TryAdd(mailId,
                    new MailData
                    {
                        ItemInstance = itinst?.Adapt<ItemInstanceDto>(),
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