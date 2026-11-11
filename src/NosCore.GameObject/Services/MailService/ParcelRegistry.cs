//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.InterChannelCommunication.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Services.MailService
{
    public class ParcelRegistry : IParcelRegistry
    {
        private readonly ConcurrentDictionary<(long CharacterId, bool IsSenderCopy), ConcurrentDictionary<long, MailData>> _parcels = new();

        public ParcelRegistry(IDao<CharacterDto, long> characterDao, IDao<MailDto, long> mailDao, List<ItemDto> items,
            IDao<IItemInstanceDto?, Guid> itemInstanceDao)
        {
            var mails = mailDao.LoadAll().ToList();
            var characterIds = mails.Select(s => s.ReceiverId)
                .Union(mails.Where(s => s.SenderId != null).Select(s => (long)s.SenderId!))
                .ToList();
            var characterNames = (characterDao.Where(s => characterIds.Contains(s.CharacterId)) ?? Enumerable.Empty<CharacterDto>())
                .ToDictionary(s => s.CharacterId, s => s.Name);
            var itemInstanceIds = mails.Where(s => s.ItemInstanceId != null).Select(s => (Guid)s.ItemInstanceId!).ToList();
            var itemInstances = (itemInstanceDao.Where(s => itemInstanceIds.Contains(s.Id)) ?? Enumerable.Empty<IItemInstanceDto?>())
                .OfType<IItemInstanceDto>()
                .ToDictionary(s => s.Id);

            foreach (var mail in mails)
            {
                var itemInstance = mail.ItemInstanceId is { } itemInstanceId
                    ? itemInstances.GetValueOrDefault(itemInstanceId)
                    : null;
                var item = itemInstance == null ? null : items.FirstOrDefault(s => s.VNum == itemInstance.ItemVNum);
                TryAddMail(mail.IsSenderCopy ? mail.SenderId ?? 0 : mail.ReceiverId, mail.IsSenderCopy,
                    new MailData
                    {
                        ItemInstance = (ItemInstanceDto?)itemInstance,
                        SenderName = mail.SenderId == null ? "NOSMALL" : characterNames.GetValueOrDefault((long)mail.SenderId),
                        ReceiverName = characterNames.GetValueOrDefault(mail.ReceiverId),
                        MailDto = mail,
                        ItemType = (short?)item?.ItemType ?? -1
                    });
            }
        }

        public ConcurrentDictionary<long, MailData> GetMails(long characterId, bool isSenderCopy) =>
            _parcels.GetOrAdd((characterId, isSenderCopy), _ => new ConcurrentDictionary<long, MailData>());

        public MailData? GetMail(long characterId, bool isSenderCopy, long mailId) =>
            GetMails(characterId, isSenderCopy).TryGetValue(mailId, out var mail) ? mail : null;

        public bool TryAddMail(long characterId, bool isSenderCopy, MailData mailData)
        {
            var mails = GetMails(characterId, isSenderCopy);
            lock (mails)
            {
                var mailId = mails.Keys.DefaultIfEmpty(-1).Max() + 1;
                if (mailId > short.MaxValue)
                {
                    return false;
                }

                mailData.MailId = mailId;
                return mails.TryAdd(mailId, mailData);
            }
        }

        public bool RemoveMail(long characterId, bool isSenderCopy, long mailId, out MailData? mailData) =>
            GetMails(characterId, isSenderCopy).TryRemove(mailId, out mailData);

        public void UpdateMail(long characterId, bool isSenderCopy, long mailId, MailData mailData) =>
            GetMails(characterId, isSenderCopy)[mailId] = mailData;
    }
}
