//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Json.Patch;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DeleteMailData = NosCore.GameObject.InterChannelCommunication.Messages.DeleteMailData;
using MailData = NosCore.GameObject.InterChannelCommunication.Messages.MailData;

namespace NosCore.GameObject.Services.MailService
{
    public class MailService(IDao<MailDto, long> mailDao, IDao<IItemInstanceDto?, Guid> itemInstanceDao,
            IPubSubHub pubSubHub, IChannelHub channelHub,
            List<ItemDto> items, IItemGenerationService itemProvider,
            IParcelRegistry parcelRegistry,
            IDao<CharacterDto, long> characterDto)
        : IMailService
    {
        public List<MailData> GetMails(long id, long characterId, bool senderCopy)
        {
            var mails = parcelRegistry.GetMails(characterId, false).Values.Concat(parcelRegistry.GetMails(characterId, true).Values);
            if (id == -1)
            {
                return mails.ToList();
            }

            var mail = parcelRegistry.GetMail(characterId, senderCopy, id);
            if (mail != null)
            {
                mails = new[] { mail };
            }
            else
            {
                return new List<MailData>();
            }

            return mails.ToList();
        }

        public async Task<bool> DeleteMailAsync(long id, long characterId, bool senderCopy)
        {
            var mail = parcelRegistry.GetMail(characterId, senderCopy, id);
            if (mail == null)
            {
                return false;
            }

            await mailDao.TryDeleteAsync(mail.MailDto.MailId);
            if (mail.ItemInstance != null)
            {
                await itemInstanceDao.TryDeleteAsync(mail.ItemInstance.Id);
            }

            if (!parcelRegistry.RemoveMail(characterId, senderCopy, id, out var maildata) || maildata == null)
            {
                return false;
            }
            var servers = await channelHub.GetCommunicationChannels();
            var accounts = await pubSubHub.GetSubscribersAsync();
            var receiver = accounts.FirstOrDefault(s => s.ConnectedCharacter?.Id == characterId && servers.Where(c => c.Type == ServerType.WorldServer).Any(x => x.Id == s.ChannelId));

            await NotifyAsync(1, receiver, maildata);
            return true;
        }

        public async Task<MailData?> EditMailAsync(long id, JsonPatch mailData)
        {
            var mail = await mailDao.FirstOrDefaultAsync(s => s.MailId == id);
            if (mail == null)
            {
                return null;
            }

            var result = mailData.Apply(JsonDocument.Parse(JsonSerializer.SerializeToUtf8Bytes(mail, new JsonSerializerOptions().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb))).RootElement);
            mail = JsonSerializer.Deserialize<MailDto>(result.GetRawText())!;
            await mailDao.TryInsertOrUpdateAsync(mail);
            var targetCharacterId = mail.IsSenderCopy ? (long)mail.SenderId! : mail.ReceiverId;
            var savedData = parcelRegistry.GetMails(targetCharacterId, mail.IsSenderCopy)
                    .FirstOrDefault(s => s.Value.MailDto.MailId == id);
            if (savedData.Value.ItemInstance == null || savedData.Value.ReceiverName == null)
            {
                return null;
            }
            var maildata = await GenerateMailDataAsync(mail, savedData.Value.ItemType, savedData.Value.ItemInstance,
                savedData.Value.ReceiverName);
            maildata.MailId = savedData.Value.MailId;
            parcelRegistry.UpdateMail(mail.IsSenderCopy ? mail.SenderId ?? 0 : mail.ReceiverId, mail.IsSenderCopy,
                savedData.Key, maildata);
            return maildata;

        }
        public async Task<bool> SendMailAsync(MailDto mail, short? vNum, short? amount, sbyte? rare, byte? upgrade)
        {
            var mailref = mail;
            var receivdto = await characterDto.FirstOrDefaultAsync(s => s.CharacterId == mailref.ReceiverId);
            if (receivdto == null)
            {
                return false;
            }

            var receiverName = receivdto.Name!;
            var it = items.Find(item => item.VNum == vNum);
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
                itemInstance = itemProvider.Create((short)vNum, amount ?? 1, rare ?? 0,
                    upgrade ?? 0);

                itemInstance = await itemInstanceDao.TryInsertOrUpdateAsync(itemInstance);
                mailref.ItemInstanceId = itemInstance?.Id;
            }

            var servers = await channelHub.GetCommunicationChannels();
            var accounts = await pubSubHub.GetSubscribersAsync();
            var receiver = accounts.FirstOrDefault(s => s.ConnectedCharacter?.Id == mailref.ReceiverId && servers.Where(c => c.Type == ServerType.WorldServer).Any(x => x.Id == s.ChannelId));

            mailref = await mailDao.TryInsertOrUpdateAsync(mailref);
            if (itemInstance == null)
            {
                return false;
            }
            var mailData = await GenerateMailDataAsync(mailref, (short?)it?.ItemType ?? -1, itemInstance, receiverName);
            parcelRegistry.AddMail(mailref.ReceiverId, mailData.MailDto.IsSenderCopy, mailData.MailId, mailData);
            await NotifyAsync(0, receiver, mailData);

            if (mailref.SenderId == null)
            {
                return true;
            }

            mailref.IsSenderCopy = true;
            mailref.MailId = 0;
            itemInstance.Id = new Guid();
            itemInstance = await itemInstanceDao.TryInsertOrUpdateAsync(itemInstance);
            mailref.ItemInstanceId = itemInstance?.Id;
            mailref = await mailDao.TryInsertOrUpdateAsync(mailref);
            var mailDataCopy = await GenerateMailDataAsync(mailref, (short?)it?.ItemType ?? -1, itemInstance!, receiverName);
            parcelRegistry.AddMail(mailref.ReceiverId, mailDataCopy.MailDto.IsSenderCopy, mailDataCopy.MailId, mailDataCopy);
            await NotifyAsync(0, receiver, mailDataCopy);

            return true;
        }

        private async Task<MailData> GenerateMailDataAsync(MailDto mailref, short itemType, IItemInstanceDto itemInstance,
            string receiverName)
        {
            var nextMailId = parcelRegistry.GetNextMailId(mailref.ReceiverId, mailref.IsSenderCopy);
            var sender = mailref.SenderId != null
                ? (await characterDto.FirstOrDefaultAsync(s => s.CharacterId == mailref.SenderId))?.Name : "NOSMALL";
            return new MailData
            {
                ReceiverName = receiverName,
                MailId = (short)nextMailId,
                MailDto = mailref,
                ItemInstance = (ItemInstanceDto)itemInstance,
                ItemType = itemType,
                SenderName = sender
            };
        }

        private async Task NotifyAsync(byte notifyType, Subscriber? receiver, MailData mailData)
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
                    await pubSubHub.SendMessageAsync(mailData);
                    break;
                case 1:
                    await pubSubHub.SendMessageAsync(new DeleteMailData()
                    {
                        CharacterId = receiver.ConnectedCharacter!.Id,
                        MailId = (short)mailData.MailId,
                        PostType = type
                    });
                    break;
            }
        }
    }
}
