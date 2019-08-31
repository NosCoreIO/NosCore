using System;
using System.Collections.Generic;
using System.Linq;
using ChickenAPI.Packets.Enumerations;
using Mapster;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using NosCore.Core;
using NosCore.Core.HttpClients.ConnectedAccountHttpClient;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Data.Enumerations.Account;
using NosCore.Data.Enumerations.Bazaar;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.I18N;
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
        private readonly IGenericDao<CharacterDto> _characterDao;
        private readonly IIncommingMailHttpClient _incommingMailHttpClient;

        public MailController(IGenericDao<MailDto> mailDao, IGenericDao<IItemInstanceDto> itemInstanceDao, IConnectedAccountHttpClient connectedAccountHttpClient,
            List<ItemDto> items, IItemProvider itemProvider, IIncommingMailHttpClient incommingMailHttpClient, IGenericDao<CharacterDto> characterDao)
        {
            _mailDao = mailDao;
            _itemInstanceDao = itemInstanceDao;
            _connectedAccountHttpClient = connectedAccountHttpClient;
            _items = items;
            _itemProvider = itemProvider;
            _characterDao = characterDao;
            _incommingMailHttpClient = incommingMailHttpClient;
        }

        [HttpGet]
        public List<MailData> GetMails(long characterId)
        {
            var listmails = new List<MailData>();
            var mails = _mailDao.Where(s => s.ReceiverId == characterId || s.SenderId == characterId).ToList();
            var idcopy = 0;
            var id = 0;
            foreach (var mail in mails)
            {
                var itinst = _itemInstanceDao.FirstOrDefault(s => s.Id == mail.ItemInstanceId);
                var it = _items.FirstOrDefault(s => s.VNum == itinst.ItemVNum);
                var senderName = mail.SenderId == null ? "NOSMALL" : _characterDao.FirstOrDefault(s=>s.CharacterId == mail.SenderId).Name;
                var receiverName = _characterDao.FirstOrDefault(s => s.CharacterId == mail.ReceiverId).Name;
                listmails.Add(new MailData
                {
                    Amount = (short)itinst.Amount,
                    SenderName = senderName,
                    ReceiverName = receiverName,
                    MailId = mail.IsSenderCopy ? (short)idcopy : (short)id,
                    Title = mail.Title,
                    Date = mail.Date,
                    AttachmentVNum = it.VNum,
                    ItemType = (short)it.ItemType,
                    IsSenderCopy = mail.IsSenderCopy
                });

                if (mail.IsSenderCopy)
                {
                    idcopy++;
                }
                else
                {
                    id++;
                }
            }
            return listmails;
        }


        [HttpDelete]
        public bool DeleteMail(long id)
        {
            throw new NotImplementedException();
        }

        [HttpPatch]
        public bool ViewMail(long id)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        public bool SendMail([FromBody] MailRequest mail)
        {
            var mailref = mail.Mail;
            var it = _items.Find(item => item.VNum == mail.VNum);
            if (mail.Mail.ItemInstanceId == Guid.Empty)
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
                IItemInstanceDto itemInstance = _itemProvider.Create((short)mail.VNum, amount: (short)mail.Amount, rare: (sbyte)mail.Rare, upgrade: (byte)mail.Upgrade);
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
                    _incommingMailHttpClient.NotifyIncommingMail(receiver.Item2.ChannelId,
                        new MailData
                        {
                            Amount = (short)mail.Amount,
                            ReceiverName = receiver.Item2.ConnectedCharacter.Name,
                            MailId = (short)idcopy,
                            Title = mail.Mail.Title,
                            Date = mail.Mail.Date,
                            AttachmentVNum = it.VNum,
                            ItemType = (short)it.ItemType,
                            IsSenderCopy = true,
                            SenderName = sender.Item2?.ConnectedCharacter.Name ?? "NOSMALL"
                        });
                }
            }

            var id = _mailDao.Where(s => s.IsSenderCopy == false && s.ReceiverId == mailref.ReceiverId).Count();
            if (receiver.Item2 != null)
            {
                _incommingMailHttpClient.NotifyIncommingMail(receiver.Item2.ChannelId,
                    new MailData
                    {
                        Amount = (short)mail.Amount,
                        ReceiverName = receiver.Item2.ConnectedCharacter.Name,
                        MailId = (short)id,
                        Title = mail.Mail.Title,
                        Date = mail.Mail.Date,
                        AttachmentVNum = it.VNum,
                        ItemType = (short)it.ItemType,
                        SenderName = sender.Item2?.ConnectedCharacter.Name ?? "NOSMALL",
                        IsSenderCopy = false
                    });
            }

            return true;
        }
    }
}
