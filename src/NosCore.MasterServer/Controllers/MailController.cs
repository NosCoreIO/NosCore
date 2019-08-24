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
using NosCore.Data.Enumerations.Account;
using NosCore.Data.Enumerations.Bazaar;
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

        public MailController(IGenericDao<MailDto> mailDao, IGenericDao<IItemInstanceDto> itemInstanceDao, IConnectedAccountHttpClient connectedAccountHttpClient, List<ItemDto> items, IItemProvider itemProvider)
        {
            _mailDao = mailDao;
            _itemInstanceDao = itemInstanceDao;
            _connectedAccountHttpClient = connectedAccountHttpClient;
            _items = items;
            _itemProvider = itemProvider;
        }

        [HttpGet]
        public List<MailDto> GetMails(long characterId)
        {
            throw new NotImplementedException();
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
        public void SendMail([FromBody] MailRequest mail)
        {
            var mailref = mail.Mail;
            if (mail.Mail.ItemInstanceId == Guid.Empty)
            {
                var it = _items.Find(item => item.VNum == mail.VNum);
                if (it == null)
                {
                    return;
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
                    return;
                }
                _itemInstanceDao.InsertOrUpdate(ref itemInstance);
                mailref.ItemInstanceId = itemInstance.Id;
            }

            _mailDao.InsertOrUpdate(ref mailref);
            if(mailref.SenderId != null)
            {
                mailref.MailId = 0;
                mailref.IsSenderCopy = true;
                _mailDao.InsertOrUpdate(ref mailref);
            }

            var receiver = _connectedAccountHttpClient.GetCharacter(mailref.ReceiverId, null);
            if (receiver.Item2 != null)
            {
                //send parcel
            }
        }
    }
}
