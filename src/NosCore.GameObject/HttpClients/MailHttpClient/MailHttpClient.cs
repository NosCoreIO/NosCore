using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.Interfaces;
using JetBrains.Annotations;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.HttpClients;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;

namespace NosCore.GameObject.HttpClients.FriendHttpClient
{
    public class MailHttpClient : MasterServerHttpClient, IMailHttpClient
    {
        private readonly ISerializer _serializer;

        public MailHttpClient(IHttpClientFactory httpClientFactory, Channel channel, IChannelHttpClient channelHttpClient,
            List<ItemDto> items, WorldConfiguration worldConfiguration, ISerializer serializer)
            : base(httpClientFactory, channel, channelHttpClient)
        {
            ApiUrl = "api/mail";
            RequireConnection = true;
            _serializer = serializer;
        }

        public MailRequest GenerateMailRequest(ICharacterEntity characterEntity, long receiverId, [CanBeNull] IItemInstanceDto itemInstance,
            short? vnum, short? amount, sbyte? rare,
            byte? upgrade, bool isNosmall)
        {
            var mail = new MailDto
            {
                IsOpened = false,
                Date = DateTime.Now,
                ReceiverId = receiverId,
                IsSenderCopy = false,
                Title = isNosmall ? "NOSMALL" : characterEntity.Name,
                SenderId = isNosmall ? (long?)null : characterEntity.VisualId,
                SenderCharacterClass = isNosmall ? (CharacterClassType?)null : characterEntity.Class,
                SenderGender = isNosmall ? (GenderType?)null : characterEntity.Gender,
                SenderHairColor = isNosmall ? (HairColorType?)null : characterEntity.HairColor,
                SenderHairStyle = isNosmall ? (HairStyleType?)null : characterEntity.HairStyle,
                EqPacket = isNosmall ? null : _serializer.Serialize(characterEntity.Equipment),
                SenderMorphId = isNosmall ? (short?)null : characterEntity.Morph == 0 ? (short)-1 : (short)(characterEntity.Morph > short.MaxValue ? 0 : characterEntity.Morph)
            };
            return new MailRequest { Mail = mail, VNum = vnum, Amount = amount, Rare = rare, Upgrade = upgrade };
        }
        public void SendGift(ICharacterEntity characterEntity, long receiverId, IItemInstanceDto itemInstance, bool isNosmall)
        {
            Post<LanguageKey>(GenerateMailRequest( characterEntity,  receiverId, itemInstance, null, null, null, null, isNosmall));

            if (characterEntity.VisualId == receiverId)
            {
                characterEntity.SendPacket(characterEntity.GenerateSay(
                    string.Format(Language.Instance.GetMessageFromKey(LanguageKey.ITEM_GIFTED, characterEntity.AccountLanguage), itemInstance.Amount), SayColorType.Green));
            }
        }

        public void SendGift(ICharacterEntity characterEntity, long receiverId, short vnum, short amount, sbyte rare,
            byte upgrade, bool isNosmall)
        {
            Post<LanguageKey>(GenerateMailRequest(characterEntity, receiverId, null, vnum, amount, rare, upgrade, isNosmall));

            if (characterEntity.VisualId == receiverId)
            {
                characterEntity.SendPacket(characterEntity.GenerateSay(
                    string.Format(Language.Instance.GetMessageFromKey(LanguageKey.ITEM_GIFTED, characterEntity.AccountLanguage), amount), SayColorType.Green));
            }
        }
    }
}
