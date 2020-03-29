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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mapster;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.HttpClients.ConnectedAccountHttpClients;
using NosCore.Core.HttpClients.IncommingMailHttpClients;
using NosCore.Data;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Account;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.MasterServer.DataHolders;

namespace NosCore.MasterServer.Controllers
{
    [Route("api/[controller]")]
    [AuthorizeRole(AuthorityType.GameMaster)]
    public class MailController : Controller
    {
        private readonly IGenericDao<CharacterDto> _characterDto;
        private readonly IConnectedAccountHttpClient _connectedAccountHttpClient;
        private readonly IIncommingMailHttpClient _incommingMailHttpClient;
        private readonly IGenericDao<IItemInstanceDto> _itemInstanceDao;
        private readonly IItemProvider _itemProvider;
        private readonly List<ItemDto> _items;
        private readonly IGenericDao<MailDto> _mailDao;
        private readonly ParcelHolder _parcelHolder;

        public MailController(IGenericDao<MailDto> mailDao, IGenericDao<IItemInstanceDto> itemInstanceDao,
            IConnectedAccountHttpClient connectedAccountHttpClient,
            List<ItemDto> items, IItemProvider itemProvider, IIncommingMailHttpClient incommingMailHttpClient,
            ParcelHolder parcelHolder,
            IGenericDao<CharacterDto> characterDto)
        {
            _mailDao = mailDao;
            _itemInstanceDao = itemInstanceDao;
            _connectedAccountHttpClient = connectedAccountHttpClient;
            _items = items;
            _itemProvider = itemProvider;
            _incommingMailHttpClient = incommingMailHttpClient;
            _parcelHolder = parcelHolder;
            _characterDto = characterDto;
        }

        [HttpGet]
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


        [HttpDelete]
        public async Task<bool> DeleteMail(long id, long characterId, bool senderCopy)
        {
            var mail = _parcelHolder[characterId][senderCopy][id];
            _mailDao.Delete(mail.MailDto.MailId);
            if (mail.ItemInstance != null)
            {
                _itemInstanceDao.Delete(mail.ItemInstance.Id);
            }

            _parcelHolder[characterId][senderCopy].TryRemove(id, out var maildata);
            if (maildata == null)
            {
                return false;
            }
            var receiver = await _connectedAccountHttpClient.GetCharacterAsync(characterId, null).ConfigureAwait(false);
            Notify(1, receiver, maildata);
            return true;
        }

        [HttpPatch]
        public MailData? ViewMail(long id, [FromBody] JsonPatchDocument<MailDto> mailData)
        {
            var mail = _mailDao.FirstOrDefault(s => s.MailId == id);
            if (mail == null)
            {
                return null;
            }


            mailData.ApplyTo(mail);
            var bz = mail;
            _mailDao.InsertOrUpdate(ref bz);
            var savedData =
                _parcelHolder[mail.IsSenderCopy ? (long)mail.SenderId! : mail.ReceiverId][mail.IsSenderCopy]
                    .FirstOrDefault(s => s.Value.MailDto.MailId == id);
            if (savedData.Value.ItemInstance == null || savedData.Value.ReceiverName == null)
            {
                return null;
            }
            var maildata = GenerateMailData(mail, savedData.Value.ItemType, savedData.Value.ItemInstance,
                savedData.Value.ReceiverName);
            maildata.MailId = savedData.Value.MailId;
            _parcelHolder[mail.IsSenderCopy ? mail.SenderId ?? 0 : mail.ReceiverId][mail.IsSenderCopy][
                savedData.Key] = maildata;
            return maildata;

        }

        [HttpPost]
        public async Task<bool> SendMail([FromBody] MailRequest mail)
        {
            if (!ModelState.IsValid)
            {
                return false;
            }
            var mailref = mail.Mail!;
            var receivdto = _characterDto.FirstOrDefault(s => s.CharacterId == mailref.ReceiverId);
            if (receivdto == null)
            {
                return false;
            }

            var receiverName = receivdto.Name!;
            var it = _items.Find(item => item.VNum == mail.VNum);
            IItemInstanceDto? itemInstance = null;
            if ((mail.Mail?.ItemInstanceId == null) && (mail.VNum != null))
            {
                if (it == null)
                {
                    return false;
                }

                if ((it.ItemType != ItemType.Weapon) && (it.ItemType != ItemType.Armor) &&
                    (it.ItemType != ItemType.Specialist))
                {
                    mail.Upgrade = 0;
                }
                else if ((it.ItemType != ItemType.Weapon) && (it.ItemType != ItemType.Armor))
                {
                    mail.Rare = 0;
                }

                if ((mail.Rare > 8) || (mail.Rare < -2))
                {
                    mail.Rare = 0;
                }

                if ((mail.Upgrade > 10) && (it.ItemType != ItemType.Specialist))
                {
                    mail.Upgrade = 0;
                }
                else if ((it.ItemType == ItemType.Specialist) && (mail.Upgrade > 15))
                {
                    mail.Upgrade = 0;
                }

                if (mail.Amount == 0)
                {
                    mail.Amount = 1;
                }

                mail.Amount = (it.Type == NoscorePocketType.Etc) || (it.Type == NoscorePocketType.Main) ? mail.Amount
                    : 1;
                itemInstance = _itemProvider.Create((short)mail.VNum, mail.Amount ?? 1, mail.Rare ?? 0,
                    mail.Upgrade ?? 0);

                _itemInstanceDao.InsertOrUpdate(ref itemInstance);
                mailref.ItemInstanceId = itemInstance.Id;
            }

            var receiver = await _connectedAccountHttpClient.GetCharacterAsync(mailref.ReceiverId, null).ConfigureAwait(false);
            var sender = await _connectedAccountHttpClient.GetCharacterAsync(mailref.SenderId, null).ConfigureAwait(false);

            _mailDao.InsertOrUpdate(ref mailref);
            if (itemInstance == null)
            {
                return false;
            }
            var mailData = GenerateMailData(mailref, (short?)it?.ItemType ?? -1, itemInstance, receiverName);
            _parcelHolder[mailref.ReceiverId][mailData.MailDto.IsSenderCopy].TryAdd(mailData.MailId, mailData);
            Notify(0, receiver, mailData);

            if (mailref.SenderId != null)
            {
                mailref.IsSenderCopy = true;
                mailref.MailId = 0;
                itemInstance.Id = new Guid();
                _itemInstanceDao.InsertOrUpdate(ref itemInstance);
                mailref.ItemInstanceId = itemInstance.Id;
                _mailDao.InsertOrUpdate(ref mailref);
                var mailDataCopy = GenerateMailData(mailref, (short?)it?.ItemType ?? -1, itemInstance, receiverName);
                _parcelHolder[mailref.ReceiverId][mailDataCopy.MailDto.IsSenderCopy]
                    .TryAdd(mailDataCopy.MailId, mailDataCopy);
                Notify(0, receiver, mailDataCopy);
            }

            return true;
        }

        private MailData GenerateMailData(MailDto mailref, short itemType, IItemInstanceDto itemInstance,
            string receiverName)
        {
            var count = _parcelHolder[mailref.ReceiverId][mailref.IsSenderCopy].Select(s => s.Key).DefaultIfEmpty(-1)
                .Max();
            var sender = mailref.SenderId != null
                ? _characterDto.FirstOrDefault(s => s.CharacterId == mailref.SenderId)?.Name : "NOSMALL";
            return new MailData
            {
                ReceiverName = receiverName,
                MailId = (short)++count,
                MailDto = mailref,
                ItemInstance = itemInstance.Adapt<ItemInstanceDto>(),
                ItemType = itemType,
                SenderName = sender
            };
        }

        private void Notify(byte notifyType, Tuple<ServerConfiguration?, ConnectedAccount?> receiver, MailData mailData)
        {
            var type = !mailData.MailDto.IsSenderCopy && (mailData.ReceiverName == receiver.Item2?.Name)
                ? mailData.ItemInstance != null ? (byte)0 : (byte)1 : (byte)2;

            if (receiver.Item2 == null)
            {
                return;
            }

            switch (notifyType)
            {
                case 0:
                    _incommingMailHttpClient.NotifyIncommingMailAsync(receiver.Item2.ChannelId, mailData);
                    break;
                case 1:
                    _incommingMailHttpClient.DeleteIncommingMailAsync(receiver.Item2.ChannelId,
                        receiver.Item2.ConnectedCharacter!.Id, (short)mailData.MailId, type);
                    break;
            }
        }
    }
}