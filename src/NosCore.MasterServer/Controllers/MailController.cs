using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Mapster;
using Microsoft.AspNetCore.Mvc;
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
        private readonly IConnectedAccountHttpClient _connectedAccountHttpClient;
        private readonly List<ItemDto> _items;
        private readonly IItemProvider _itemProvider;
        private readonly IIncommingMailHttpClient _incommingMailHttpClient;
        private readonly ParcelHolder _parcelHolder;

        public MailController(IGenericDao<MailDto> mailDao, IGenericDao<IItemInstanceDto> itemInstanceDao, IConnectedAccountHttpClient connectedAccountHttpClient,
            List<ItemDto> items, IItemProvider itemProvider, IIncommingMailHttpClient incommingMailHttpClient, ParcelHolder parcelHolder)
        {
            _mailDao = mailDao;
            _itemInstanceDao = itemInstanceDao;
            _connectedAccountHttpClient = connectedAccountHttpClient;
            _items = items;
            _itemProvider = itemProvider;
            _incommingMailHttpClient = incommingMailHttpClient;
            _parcelHolder = parcelHolder;
        }

        [HttpGet]
        public List<MailData> GetMails(long id, long characterId)
        {
            var mails = _parcelHolder.ParcelDictionary[characterId][false].Values.Concat(_parcelHolder.ParcelDictionary[characterId][true].Values);
            if (id != -1)
            {
                mails = new[] { _parcelHolder.ParcelDictionary[characterId][false][id] };
            }
            return mails.ToList();
        }


        [HttpDelete]
        public bool DeleteMail(long id, long characterId, bool senderCopy)
        {
            var mail = _parcelHolder.ParcelDictionary[characterId][senderCopy][id];
            _mailDao.Delete(mail.MailId);
            if (mail.ItemInstance != null)
            {
                _itemInstanceDao.Delete(mail.ItemInstance.Id);
            }
            return true;
        }

        [HttpPatch]
        public bool ViewMail(long id, long characterId, bool senderCopy)
        {
            var mail = _parcelHolder.ParcelDictionary[characterId][senderCopy][id];
            var mailDto = _mailDao.FirstOrDefault(s=>s.MailId == mail.MailId);
            if (mailDto != null)
            {
                _mailDao.InsertOrUpdate(ref mailDto);
                return true;
            }

            return false;
        }

        [HttpPost]
        public bool SendMail([FromBody] MailRequest mail)
        {
            var mailref = mail.Mail;
            var it = _items.Find(item => item.VNum == mail.VNum);
            IItemInstanceDto itemInstance = null;
            if (mail.Mail.ItemInstanceId == null)
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

            _mailDao.InsertOrUpdate(ref mailref);
            var receiver = _connectedAccountHttpClient.GetCharacter(mailref.ReceiverId, null);
            var sender = _connectedAccountHttpClient.GetCharacter(mailref.SenderId, null);

            if (mailref.SenderId != null && mailref.SenderId != mailref.ReceiverId)
            {
                mailref.MailId = 0;
                mailref.IsSenderCopy = true;
                _mailDao.InsertOrUpdate(ref mailref);

                var idcopy = _mailDao.Where(s => s.IsSenderCopy == true && s.SenderId == mailref.SenderId).Count();
                if (receiver.Item2 != null)
                {
                    Notify(receiver.Item2.ChannelId,
                        new MailData
                        {
                            ReceiverName = receiver.Item2.ConnectedCharacter.Name,
                            MailId = (short)idcopy,
                            Title = mail.Mail.Title,
                            Date = mail.Mail.Date,
                            ItemInstance = itemInstance.Adapt<ItemInstanceDto>(),
                            ItemType = (short)it.ItemType,
                            IsSenderCopy = true,
                            SenderName = sender.Item2?.ConnectedCharacter.Name ?? "NOSMALL"
                        });
                }
            }

            var id = _mailDao.Where(s => s.IsSenderCopy == false && s.ReceiverId == mailref.ReceiverId).Count();
            if (receiver.Item2 != null)
            {
                Notify(receiver.Item2.ChannelId,
                    new MailData
                    {
                        ReceiverName = receiver.Item2.ConnectedCharacter.Name,
                        MailId = (short) id,
                        Title = mail.Mail.Title,
                        Date = mail.Mail.Date,
                        ItemInstance = itemInstance.Adapt<ItemInstanceDto>(),
                        ItemType = (short) it.ItemType,
                        SenderName = sender.Item2?.ConnectedCharacter.Name ?? "NOSMALL",
                        IsSenderCopy = false
                    });
            }

            return true;
        }

        private void Notify(int channelId, MailData mailData)
        {
            //todo add to holder
            _incommingMailHttpClient.NotifyIncommingMail(channelId, mailData);
        }
    }
}
