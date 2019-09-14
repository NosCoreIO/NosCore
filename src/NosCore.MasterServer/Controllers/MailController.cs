using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Mapster;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.HttpClients.ConnectedAccountHttpClient;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Data.Enumerations.Account;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.WebApi;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.MasterServer.DataHolders;

namespace NosCore.MasterServer.Controllers
{
    [Route("api/[controller]")]
    [AuthorizeRole(AuthorityType.GameMaster)]
    public class MailController : Controller
    {
        private readonly IGenericDao<MailDto> _mailDao;
        private readonly IGenericDao<IItemInstanceDto> _itemInstanceDao;
        private readonly IGenericDao<CharacterDto> _characterDto;
        private readonly IConnectedAccountHttpClient _connectedAccountHttpClient;
        private readonly List<ItemDto> _items;
        private readonly IItemProvider _itemProvider;
        private readonly IIncommingMailHttpClient _incommingMailHttpClient;
        private readonly ParcelHolder _parcelHolder;

        public MailController(IGenericDao<MailDto> mailDao, IGenericDao<IItemInstanceDto> itemInstanceDao, IConnectedAccountHttpClient connectedAccountHttpClient,
                List<ItemDto> items, IItemProvider itemProvider, IIncommingMailHttpClient incommingMailHttpClient, ParcelHolder parcelHolder,
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
        public List<MailData> GetMails(long id, long characterId)
        {
            var mails = _parcelHolder[characterId][false].Values.Concat(_parcelHolder[characterId][true].Values);
            if (id != -1)
            {
                if (_parcelHolder[characterId][false].ContainsKey(id))
                {
                    mails = new[] { _parcelHolder[characterId][false][id] };
                }
                else
                {
                    return new List<MailData>();
                }
            }
            return mails.ToList();
        }


        [HttpDelete]
        public bool DeleteMail(long id, long characterId, bool senderCopy)
        {
            var mail = _parcelHolder[characterId][senderCopy][id];
            _mailDao.Delete(mail.MailDbKey);
            if (mail.ItemInstance != null)
            {
                _itemInstanceDao.Delete(mail.ItemInstance.Id);
            }

            _parcelHolder[characterId][senderCopy].TryRemove(id, out _);
            return true;
        }

        [HttpPatch]
        public MailData ViewMail(long dbKey, [FromBody]JsonPatchDocument<MailDto> mailData)
        {
            var mail = _mailDao.FirstOrDefault(s => s.MailId == dbKey);
            if (mail != null)
            {
                mailData.ApplyTo(mail);
                var bz = mail;
                _mailDao.InsertOrUpdate(ref bz);
                var maildata = _parcelHolder[mail.IsSenderCopy ? (long)mail.SenderId : mail.ReceiverId][mail.IsSenderCopy].FirstOrDefault();
                maildata.Value.IsOpened = true;
                return maildata.Value;
            }
            return null;
        }

        [HttpPost]
        public bool SendMail([FromBody] MailRequest mail)
        {
            var mailref = mail.Mail;
            var receivdto = _characterDto.FirstOrDefault(s => s.CharacterId == mailref.ReceiverId);
            if (receivdto == null)
            {
                return false;
            }
            var receiverName = receivdto.Name;
            var it = _items.Find(item => item.VNum == mail.VNum);
            IItemInstanceDto itemInstance = null;
            if (mail.Mail.ItemInstanceId == null && mail.VNum != null)
            {
                if (it == null)
                {
                    return false;
                }
                if (it.ItemType != ItemType.Weapon && it.ItemType != ItemType.Armor && it.ItemType != ItemType.Specialist)
                {
                    mail.Upgrade = 0;
                }
                else if (it.ItemType != ItemType.Weapon && it.ItemType != ItemType.Armor)
                {
                    mail.Rare = 0;
                }

                if (mail.Rare > 8 || mail.Rare < -2)
                {
                    mail.Rare = 0;
                }
                if (mail.Upgrade > 10 && it.ItemType != ItemType.Specialist)
                {
                    mail.Upgrade = 0;
                }
                else if (it.ItemType == ItemType.Specialist && mail.Upgrade > 15)
                {
                    mail.Upgrade = 0;
                }

                if (mail.Amount == 0)
                {
                    mail.Amount = 1;
                }
                mail.Amount = it.Type == NoscorePocketType.Etc || it.Type == NoscorePocketType.Main ? mail.Amount : 1;
                itemInstance = _itemProvider.Create((short)mail.VNum, amount: (short)mail.Amount, rare: (sbyte)mail.Rare, upgrade: (byte)mail.Upgrade);
                if (itemInstance == null)
                {
                    return false;
                }
                _itemInstanceDao.InsertOrUpdate(ref itemInstance);
                mailref.ItemInstanceId = itemInstance.Id;
            }

            var receiver = _connectedAccountHttpClient.GetCharacter(mailref.ReceiverId, null);
            var sender = _connectedAccountHttpClient.GetCharacter(mailref.SenderId, null);

            InsertAndNotify(receiver, sender, mailref, mailref.IsSenderCopy, it, itemInstance, receiverName);
            if (mailref.SenderId != null && mailref.SenderId != mailref.ReceiverId)
            {
                mailref.MailId = 0;
                mailref.IsSenderCopy = true;
                InsertAndNotify(receiver, sender, mailref, mailref.IsSenderCopy, it, itemInstance, receiverName);
            }

            return true;
        }

        private void InsertAndNotify((ServerConfiguration, ConnectedAccount) receiver, (ServerConfiguration, ConnectedAccount) sender,
            MailDto mailref, bool isSenderCopy, ItemDto it, IItemInstanceDto itemInstance, string receiverName)
        {
            var count = _parcelHolder[mailref.ReceiverId][isSenderCopy].Select(s => s.Key).DefaultIfEmpty(-1).Max();
            _mailDao.InsertOrUpdate(ref mailref);
            var mailData = new MailData
            {
                ReceiverName = receiverName,
                MailId = (short)++count,
                Title = mailref.Title,
                Message = mailref.Message,
                Date = mailref.Date,
                ItemInstance = itemInstance.Adapt<ItemInstanceDto>(),
                ItemType = (short?)it?.ItemType ?? -1,
                SenderName = sender.Item2?.ConnectedCharacter.Name ?? "NOSMALL",
                IsSenderCopy = isSenderCopy,
                MailDbKey = mailref.MailId
            };
            _parcelHolder[mailref.ReceiverId][mailData.IsSenderCopy].TryAdd(count, mailData);
            if (receiver.Item2 != null)
            {
                _incommingMailHttpClient.NotifyIncommingMail(receiver.Item2.ChannelId, mailData);
            }
        }
    }
}
