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

using Json.Patch;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.GameObject.Holders;
using NosCore.GameObject.Services.ItemGenerationService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NosCore.GameObject.HubClients.ChannelHubClient;
using NosCore.GameObject.HubClients.MailHubClient;

namespace NosCore.GameObject.Services.MailService
{
    public class MailService : IMailService
    {
        private readonly IDao<CharacterDto, long> _characterDto;
        private readonly IMailHubClient _incommingMailHttpClient;
        private readonly IDao<IItemInstanceDto?, Guid> _itemInstanceDao;
        private readonly IItemGenerationService _itemProvider;
        private readonly List<ItemDto> _items;
        private readonly IDao<MailDto, long> _mailDao;
        private readonly ParcelHolder _parcelHolder;
        private readonly IChannelHubClient _channelHubClient;

        public MailService(IDao<MailDto, long> mailDao, IDao<IItemInstanceDto?, Guid> itemInstanceDao,
            IChannelHubClient channelHubClient,
            List<ItemDto> items, IItemGenerationService itemProvider, IMailHubClient incommingMailHttpClient,
            ParcelHolder parcelHolder,
            IDao<CharacterDto, long> characterDto)
        {
            _mailDao = mailDao;
            _itemInstanceDao = itemInstanceDao;
            _channelHubClient = channelHubClient;
            _items = items;
            _itemProvider = itemProvider;
            _incommingMailHttpClient = incommingMailHttpClient;
            _parcelHolder = parcelHolder;
            _characterDto = characterDto;
        }

        public List<MailData> GetMails(long id, long characterId, bool senderCopy)
        {
            var mails = _parcelHolder[characterId][false].Values.Concat(_parcelHolder[characterId][true].Values);
            if (id == -1)
            {
                return mails.ToList();
            }

            if (_parcelHolder[characterId][senderCopy].ContainsKey(id))
            {
                mails = new[] { _parcelHolder[characterId][senderCopy][id] };
            }
            else
            {
                return new List<MailData>();
            }

            return mails.ToList();
        }

        public async Task<bool> DeleteMailAsync(long id, long characterId, bool senderCopy)
        {
            var mail = _parcelHolder[characterId][senderCopy][id];
            await _mailDao.TryDeleteAsync(mail.MailDto.MailId).ConfigureAwait(false);
            if (mail.ItemInstance != null)
            {
                await _itemInstanceDao.TryDeleteAsync(mail.ItemInstance.Id).ConfigureAwait(false);
            }

            _parcelHolder[characterId][senderCopy].TryRemove(id, out var maildata);
            if (maildata == null)
            {
                return false;
            }
            var receiver = await _channelHubClient.GetCharacterAsync(characterId, null).ConfigureAwait(false);
            await NotifyAsync(1, receiver, maildata).ConfigureAwait(false);
            return true;
        }

        public async Task<MailData?> EditMailAsync(long id, JsonPatch mailData)
        {
            var mail = await _mailDao.FirstOrDefaultAsync(s => s.MailId == id).ConfigureAwait(false);
            if (mail == null)
            {
                return null;
            }

            var result = mailData.Apply(JsonDocument.Parse(JsonSerializer.SerializeToUtf8Bytes(mail)).RootElement);
            mail = JsonSerializer.Deserialize<MailDto>(result!.Result.GetRawText())!;
            await _mailDao.TryInsertOrUpdateAsync(mail).ConfigureAwait(false);
            var savedData =
                _parcelHolder[mail.IsSenderCopy ? (long)mail.SenderId! : mail.ReceiverId][mail.IsSenderCopy]
                    .FirstOrDefault(s => s.Value.MailDto.MailId == id);
            if (savedData.Value.ItemInstance == null || savedData.Value.ReceiverName == null)
            {
                return null;
            }
            var maildata = await GenerateMailDataAsync(mail, savedData.Value.ItemType, savedData.Value.ItemInstance,
                savedData.Value.ReceiverName).ConfigureAwait(false);
            maildata.MailId = savedData.Value.MailId;
            _parcelHolder[mail.IsSenderCopy ? mail.SenderId ?? 0 : mail.ReceiverId][mail.IsSenderCopy][
                savedData.Key] = maildata;
            return maildata;

        }
        public async Task<bool> SendMailAsync(MailDto mail, short? vNum, short? amount, sbyte? rare, byte? upgrade)
        {
            var mailref = mail;
            var receivdto = await _characterDto.FirstOrDefaultAsync(s => s.CharacterId == mailref.ReceiverId).ConfigureAwait(false);
            if (receivdto == null)
            {
                return false;
            }

            var receiverName = receivdto.Name!;
            var it = _items.Find(item => item.VNum == vNum);
            IItemInstanceDto? itemInstance = null;
            if ((mail?.ItemInstanceId == null) && (vNum != null))
            {
                if (it == null)
                {
                    return false;
                }

                if ((it.ItemType != ItemType.Weapon) && (it.ItemType != ItemType.Armor) &&
                    (it.ItemType != ItemType.Specialist))
                {
                    upgrade = 0;
                }
                else if ((it.ItemType != ItemType.Weapon) && (it.ItemType != ItemType.Armor))
                {
                    rare = 0;
                }

                if ((rare > 8) || (rare < -2))
                {
                    rare = 0;
                }

                if ((upgrade > 10) && (it.ItemType != ItemType.Specialist))
                {
                    upgrade = 0;
                }
                else if ((it.ItemType == ItemType.Specialist) && (upgrade > 15))
                {
                    upgrade = 0;
                }

                if (amount == 0)
                {
                    amount = 1;
                }

                amount = (it.Type == NoscorePocketType.Etc) || (it.Type == NoscorePocketType.Main) ? amount
                    : 1;
                itemInstance = _itemProvider.Create((short)vNum, amount ?? 1, rare ?? 0,
                    upgrade ?? 0);

                itemInstance = await _itemInstanceDao.TryInsertOrUpdateAsync(itemInstance).ConfigureAwait(false);
                mailref.ItemInstanceId = itemInstance?.Id;
            }

            var receiver = await _channelHubClient.GetCharacterAsync(mailref.ReceiverId, null).ConfigureAwait(false);
            var sender = await _channelHubClient.GetCharacterAsync(mailref.SenderId, null).ConfigureAwait(false);
            if (receiver == null || sender == null)
            {
                return false;
            }
            mailref = await _mailDao.TryInsertOrUpdateAsync(mailref).ConfigureAwait(false);
            if (itemInstance == null)
            {
                return false;
            }
            var mailData = await GenerateMailDataAsync(mailref, (short?)it?.ItemType ?? -1, itemInstance, receiverName).ConfigureAwait(false);
            _parcelHolder[mailref.ReceiverId][mailData.MailDto.IsSenderCopy].TryAdd(mailData.MailId, mailData);
            await NotifyAsync(0, receiver, mailData).ConfigureAwait(false);

            if (mailref.SenderId == null)
            {
                return true;
            }

            mailref.IsSenderCopy = true;
            mailref.MailId = 0;
            itemInstance.Id = new Guid();
            itemInstance = await _itemInstanceDao.TryInsertOrUpdateAsync(itemInstance).ConfigureAwait(false);
            mailref.ItemInstanceId = itemInstance?.Id;
            mailref = await _mailDao.TryInsertOrUpdateAsync(mailref).ConfigureAwait(false);
            var mailDataCopy = await GenerateMailDataAsync(mailref, (short?)it?.ItemType ?? -1, itemInstance!, receiverName).ConfigureAwait(false);
            _parcelHolder[mailref.ReceiverId][mailDataCopy.MailDto.IsSenderCopy]
                .TryAdd(mailDataCopy.MailId, mailDataCopy);
            await NotifyAsync(0, receiver, mailDataCopy).ConfigureAwait(false);

            return true;
        }

        private async Task<MailData> GenerateMailDataAsync(MailDto mailref, short itemType, IItemInstanceDto itemInstance,
            string receiverName)
        {
            var count = _parcelHolder[mailref.ReceiverId][mailref.IsSenderCopy].Select(s => s.Key).DefaultIfEmpty(-1)
                .Max();
            var sender = mailref.SenderId != null
                ? (await _characterDto.FirstOrDefaultAsync(s => s.CharacterId == mailref.SenderId).ConfigureAwait(false))?.Name : "NOSMALL";
            return new MailData
            {
                ReceiverName = receiverName,
                MailId = (short)++count,
                MailDto = mailref,
                ItemInstance = (ItemInstanceDto)itemInstance,
                ItemType = itemType,
                SenderName = sender
            };
        }

        private async Task NotifyAsync(byte notifyType, ConnectedAccount? receiver, MailData mailData)
        {
            var type = !mailData.MailDto.IsSenderCopy && (mailData.ReceiverName == receiver?.Name)
                ? mailData.ItemInstance != null ? (byte)0 : (byte)1 : (byte)2;

            if (receiver == null)
            {
                return;
            }

            switch (notifyType)
            {
                case 0:
                    await _incommingMailHttpClient.NotifyIncommingMailAsync(receiver.ChannelId, mailData).ConfigureAwait(false);
                    break;
                case 1:
                    await _incommingMailHttpClient.DeleteIncommingMailAsync(receiver.ChannelId,
                        receiver.ConnectedCharacter!.Id, (short)mailData.MailId, type).ConfigureAwait(false);
                    break;
            }
        }
    }
}