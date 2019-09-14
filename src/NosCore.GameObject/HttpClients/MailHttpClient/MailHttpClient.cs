using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.Interfaces;
using JetBrains.Annotations;
using Microsoft.AspNetCore.JsonPatch;
using NosCore.Core;
using NosCore.Core.HttpClients;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Data;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Interfaces;

namespace NosCore.GameObject.HttpClients.FriendHttpClient
{
    public class MailHttpClient : MasterServerHttpClient, IMailHttpClient
    {
        public MailHttpClient(IHttpClientFactory httpClientFactory, Channel channel, IChannelHttpClient channelHttpClient, ISerializer serializer)
            : base(httpClientFactory, channel, channelHttpClient)
        {
            ApiUrl = "api/mail";
            RequireConnection = true;
        }

        private MailRequest GenerateMailRequest(ICharacterEntity characterEntity, long receiverId, [CanBeNull] IItemInstanceDto itemInstance,
            short? vnum, short? amount, sbyte? rare,
            byte? upgrade, bool isNosmall, string title, string text)
        {
            var mail = new MailDto
            {
                IsOpened = false,
                Date = DateTime.Now,
                ReceiverId = receiverId,
                IsSenderCopy = false,
                ItemInstanceId = itemInstance?.Id,
                Title = isNosmall ? "NOSMALL" : title ?? characterEntity.Name,
                Message = text,
                SenderId = isNosmall ? (long?)null : characterEntity.VisualId,
                SenderCharacterClass = isNosmall ? (CharacterClassType?)null : characterEntity.Class,
                SenderGender = isNosmall ? (GenderType?)null : characterEntity.Gender,
                SenderHairColor = isNosmall ? (HairColorType?)null : characterEntity.HairColor,
                SenderHairStyle = isNosmall ? (HairStyleType?)null : characterEntity.HairStyle,
                Hat = isNosmall ? null : characterEntity.Equipment.Hat,
                Armor = isNosmall ? null : characterEntity.Equipment.Armor,
                MainWeapon = isNosmall ? null : characterEntity.Equipment.MainWeapon,
                SecondaryWeapon = isNosmall ? null : characterEntity.Equipment.SecondaryWeapon,
                Mask = isNosmall ? null : characterEntity.Equipment.Mask,
                Fairy = isNosmall ? null : characterEntity.Equipment.Fairy,
                CostumeSuit = isNosmall ? null : characterEntity.Equipment.CostumeSuit,
                CostumeHat = isNosmall ? null : characterEntity.Equipment.CostumeHat,
                WeaponSkin = isNosmall ? null : characterEntity.Equipment.WeaponSkin,
                WingSkin = isNosmall ? null : characterEntity.Equipment.WingSkin,
                SenderMorphId = isNosmall ? (short?)null : characterEntity.Morph == 0 ? (short)-1 : (short)(characterEntity.Morph > short.MaxValue ? 0 : characterEntity.Morph)
            };
            return new MailRequest { Mail = mail, VNum = vnum, Amount = amount, Rare = rare, Upgrade = upgrade };
        }
        public void SendGift(ICharacterEntity characterEntity, long receiverId, IItemInstanceDto itemInstance, bool isNosmall)
        {
            Post<bool>(GenerateMailRequest(characterEntity, receiverId, itemInstance, null, null, null, null, isNosmall, null, null));
        }

        public void SendGift(ICharacterEntity characterEntity, long receiverId, short vnum, short amount, sbyte rare,
            byte upgrade, bool isNosmall)
        {
            Post<bool>(GenerateMailRequest(characterEntity, receiverId, null, vnum, amount, rare, upgrade, isNosmall, null, null));
        }

        public void SendMessage(ICharacterEntity characterEntity, long receiverId, string title, string text)
        {
            Post<bool>(GenerateMailRequest(characterEntity, receiverId, null, null, null, null, null, false, title, text));
        }

        public IEnumerable<MailData> GetGifts(long characterId)
        {
            return Get<IEnumerable<MailData>>($"-1&characterId={characterId}");
        }

        public MailData GetGift(long id, long characterId)
        {
            return Get<IEnumerable<MailData>>($"{id}&characterId={characterId}").FirstOrDefault();
        }

        public void DeleteGift(long giftId, long visualId)
        {
            Delete($"{giftId}&characterId={visualId}").Wait();
        }

        public void ViewGift(long giftId, JsonPatchDocument<MailDto> mailData)
        {
            Patch<MailData>(giftId, mailData);
        }
    }
}
