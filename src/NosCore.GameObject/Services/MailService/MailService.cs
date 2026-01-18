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
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.GameObject.Services.ItemGenerationService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Shared.Enumerations;
using DeleteMailData = NosCore.GameObject.InterChannelCommunication.Messages.DeleteMailData;

using MailData = NosCore.GameObject.InterChannelCommunication.Messages.MailData;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;

namespace NosCore.GameObject.Services.MailService
{
    public class MailService(IDao<MailDto, long> mailDao, IDao<IItemInstanceDto?, Guid> itemInstanceDao,
            IPubSubHub pubSubHub, IChannelHub channelHub,
            List<ItemDto> items, IItemGenerationService itemProvider,
            IParcelRegistry parcelRegistry,
            IDao<CharacterDto, long> characterDto)
        : IMailService
    {
        public async Task InitializeAsync(IDao<CharacterDto, long> charDao, IDao<MailDto, long> mailDaoParam, List<ItemDto> itemsList,
            IDao<IItemInstanceDto?, Guid> itemInstDao)
        {
            var mails = mailDaoParam.LoadAll().ToList();
            var idcopy = 0;
            var idmail = 0;
            var charactersIds = mails.Select(s => s.ReceiverId)
                .Union(mails.Where(s => s.SenderId != null).Select(s => (long)s.SenderId!));
            var characternames = new Dictionary<long, string?>();
            foreach (var characterId in charactersIds)
            {
                characternames.Add(characterId, (await charDao.FirstOrDefaultAsync(s => s.CharacterId == characterId).ConfigureAwait(false))?.Name);
            }

            foreach (var mail in mails)
            {
                var itinst = await itemInstDao.FirstOrDefaultAsync(s => s!.Id == mail.ItemInstanceId).ConfigureAwait(false);
                ItemDto? it = null;
                if (itinst != null)
                {
                    it = itemsList.FirstOrDefault(s => s.VNum == itinst.ItemVNum);
                }

                var senderName = mail.SenderId == null ? "NOSMALL" : characternames[(long)mail.SenderId];
                var receiverName = characternames[mail.ReceiverId];
                var mailId = mail.IsSenderCopy ? (short)idcopy : (short)idmail;
                parcelRegistry.TryAdd(mail.IsSenderCopy ? mail.SenderId ?? 0 : mail.ReceiverId, mail.IsSenderCopy, mailId,
                    new MailData
                    {
                        ItemInstance = (ItemInstanceDto?)itinst,
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

        public List<MailData> GetMails(long id, long characterId, bool senderCopy)
        {
            var mails = parcelRegistry.GetMails(characterId);
            if (id == -1)
            {
                return mails.ToList();
            }

            if (parcelRegistry.ContainsMail(characterId, senderCopy, id))
            {
                var mail = parcelRegistry.GetMail(characterId, senderCopy, id);
                return mail != null ? new List<MailData> { mail } : new List<MailData>();
            }

            return new List<MailData>();
        }

        public async Task<bool> DeleteMailAsync(long id, long characterId, bool senderCopy)
        {
            var mail = parcelRegistry.GetMail(characterId, senderCopy, id);
            if (mail == null)
            {
                return false;
            }

            await mailDao.TryDeleteAsync(mail.MailDto.MailId).ConfigureAwait(false);
            if (mail.ItemInstance != null)
            {
                await itemInstanceDao.TryDeleteAsync(mail.ItemInstance.Id).ConfigureAwait(false);
            }

            parcelRegistry.TryRemove(characterId, senderCopy, id, out var maildata);
            if (maildata == null)
            {
                return false;
            }
            var servers = await channelHub.GetCommunicationChannels();
            var accounts = await pubSubHub.GetSubscribersAsync();
            var receiver = accounts.FirstOrDefault(s => s.ConnectedCharacter?.Id == characterId && servers.Where(c => c.Type == ServerType.WorldServer).Any(x => x.Id == s.ChannelId));

            await NotifyAsync(1, receiver, maildata).ConfigureAwait(false);
            return true;
        }

        public async Task<MailData?> EditMailAsync(long id, JsonPatch mailData)
        {
            var mail = await mailDao.FirstOrDefaultAsync(s => s.MailId == id).ConfigureAwait(false);
            if (mail == null)
            {
                return null;
            }

            var result = mailData.Apply(JsonDocument.Parse(JsonSerializer.SerializeToUtf8Bytes(mail, new JsonSerializerOptions().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb))).RootElement);
            mail = JsonSerializer.Deserialize<MailDto>(result.GetRawText())!;
            await mailDao.TryInsertOrUpdateAsync(mail).ConfigureAwait(false);
            var savedData = parcelRegistry.FindMailByMailDtoId(mail.IsSenderCopy ? (long)mail.SenderId! : mail.ReceiverId, mail.IsSenderCopy, id);
            if (savedData?.Value.ItemInstance == null || savedData?.Value.ReceiverName == null)
            {
                return null;
            }
            var maildata = await GenerateMailDataAsync(mail, savedData.Value.Value.ItemType, savedData.Value.Value.ItemInstance,
                savedData.Value.Value.ReceiverName).ConfigureAwait(false);
            maildata.MailId = savedData.Value.Value.MailId;
            parcelRegistry.Update(mail.IsSenderCopy ? mail.SenderId ?? 0 : mail.ReceiverId, mail.IsSenderCopy, savedData.Value.Key, maildata);
            return maildata;

        }
        public async Task<bool> SendMailAsync(MailDto mail, short? vNum, short? amount, sbyte? rare, byte? upgrade)
        {
            var mailref = mail;
            var receivdto = await characterDto.FirstOrDefaultAsync(s => s.CharacterId == mailref.ReceiverId).ConfigureAwait(false);
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

                itemInstance = await itemInstanceDao.TryInsertOrUpdateAsync(itemInstance).ConfigureAwait(false);
                mailref.ItemInstanceId = itemInstance?.Id;
            }

            var servers = await channelHub.GetCommunicationChannels();
            var accounts = await pubSubHub.GetSubscribersAsync();
            var receiver = accounts.FirstOrDefault(s => s.ConnectedCharacter?.Id == mailref.ReceiverId && servers.Where(c => c.Type == ServerType.WorldServer).Any(x => x.Id == s.ChannelId));

            mailref = await mailDao.TryInsertOrUpdateAsync(mailref).ConfigureAwait(false);
            if (itemInstance == null)
            {
                return false;
            }
            var mailData = await GenerateMailDataAsync(mailref, (short?)it?.ItemType ?? -1, itemInstance, receiverName).ConfigureAwait(false);
            parcelRegistry.TryAdd(mailref.ReceiverId, mailData.MailDto.IsSenderCopy, mailData.MailId, mailData);
            await NotifyAsync(0, receiver, mailData).ConfigureAwait(false);

            if (mailref.SenderId == null)
            {
                return true;
            }

            mailref.IsSenderCopy = true;
            mailref.MailId = 0;
            itemInstance.Id = new Guid();
            itemInstance = await itemInstanceDao.TryInsertOrUpdateAsync(itemInstance).ConfigureAwait(false);
            mailref.ItemInstanceId = itemInstance?.Id;
            mailref = await mailDao.TryInsertOrUpdateAsync(mailref).ConfigureAwait(false);
            var mailDataCopy = await GenerateMailDataAsync(mailref, (short?)it?.ItemType ?? -1, itemInstance!, receiverName).ConfigureAwait(false);
            parcelRegistry.TryAdd(mailref.ReceiverId, mailDataCopy.MailDto.IsSenderCopy, mailDataCopy.MailId, mailDataCopy);
            await NotifyAsync(0,  receiver, mailDataCopy).ConfigureAwait(false);

            return true;
        }

        private async Task<MailData> GenerateMailDataAsync(MailDto mailref, short itemType, IItemInstanceDto itemInstance,
            string receiverName)
        {
            var count = parcelRegistry.GetMaxMailId(mailref.ReceiverId, mailref.IsSenderCopy);
            var sender = mailref.SenderId != null
                ? (await characterDto.FirstOrDefaultAsync(s => s.CharacterId == mailref.SenderId).ConfigureAwait(false))?.Name : "NOSMALL";
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
                    await pubSubHub.SendMessageAsync(mailData).ConfigureAwait(false);
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

        public async Task GenerateMailAsync(IPacketSender sender, long characterId, IEnumerable<MailData> mails)
        {
            foreach (var mail in mails)
            {
                if (!mail.MailDto.IsSenderCopy && mail.MailDto.ReceiverId == characterId)
                {
                    var mailType = mail.MailDto.ItemInstanceId != null ? (byte)0 : (byte)1;
                    parcelRegistry.TryAdd(characterId, false, mail.MailId, mail);
                    var packet = mail.GeneratePost(mailType);
                    if (packet != null)
                    {
                        await sender.SendPacketAsync(packet).ConfigureAwait(false);
                    }
                }
                else if (mail.MailDto.IsSenderCopy && mail.MailDto.SenderId == characterId)
                {
                    parcelRegistry.TryAdd(characterId, true, mail.MailId, mail);
                    var packet = mail.GeneratePost(2);
                    if (packet != null)
                    {
                        await sender.SendPacketAsync(packet).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}