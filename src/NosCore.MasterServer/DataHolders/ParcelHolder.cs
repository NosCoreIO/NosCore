using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Mapster;
using NosCore.Core;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Data.WebApi;

namespace NosCore.MasterServer.DataHolders
{
    public class ParcelHolder
    {
        private readonly IGenericDao<MailDto> _mailDao;
        private readonly List<ItemDto> _items;
        private readonly IGenericDao<CharacterDto> _characterDao;
        private readonly IGenericDao<IItemInstanceDto> _itemInstanceDao;

        public ConcurrentDictionary<long, ConcurrentDictionary<bool, ConcurrentDictionary<long, MailData>>> ParcelDictionary { get; set; } 
            = new ConcurrentDictionary<long, ConcurrentDictionary<bool, ConcurrentDictionary<long, MailData>>>();

        public ParcelHolder(IGenericDao<CharacterDto> characterDao, IGenericDao<MailDto> mailDao, List<ItemDto> items, IGenericDao<IItemInstanceDto> itemInstanceDao)
        {
            _mailDao = mailDao;
            _items = items;
            _characterDao = characterDao;
            _itemInstanceDao = itemInstanceDao;
            Initialize();
        }

        private void Initialize()
        {
            var mails = _mailDao.LoadAll();
            var idcopy = 0;
            var idmail = 0;
            var listmails = new List<MailData>();
            foreach (var mail in mails)
            {
                var itinst = _itemInstanceDao.FirstOrDefault(s => s.Id == mail.ItemInstanceId);
                var it = _items.FirstOrDefault(s => s.VNum == itinst.ItemVNum);
                var senderName = mail.SenderId == null ? "NOSMALL" : _characterDao.FirstOrDefault(s => s.CharacterId == mail.SenderId).Name;
                var receiverName = _characterDao.FirstOrDefault(s => s.CharacterId == mail.ReceiverId).Name;
                listmails.Add(new MailData
                {
                    ItemInstance = itinst.Adapt<ItemInstanceDto>(),
                    SenderName = senderName,
                    ReceiverName = receiverName,
                    MailId = mail.IsSenderCopy ? (short)idcopy : (short)idmail,
                    Title = mail.Title,
                    Date = mail.Date,
                    ItemType = (short)it.ItemType,
                    IsSenderCopy = mail.IsSenderCopy
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
