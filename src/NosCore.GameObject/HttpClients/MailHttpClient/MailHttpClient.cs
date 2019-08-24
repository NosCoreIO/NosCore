using System;
using System.Collections.Generic;
using System.Net.Http;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.Interfaces;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.HttpClients;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Data;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Interfaces;

namespace NosCore.GameObject.HttpClients.FriendHttpClient
{
    public class MailHttpClient : MasterServerHttpClient, IMailHttpClient
    {
        private readonly List<ItemDto> _items;
        private readonly ISerializer _serializer;
        private readonly WorldConfiguration _worldConfiguration;

        public MailHttpClient(IHttpClientFactory httpClientFactory, Channel channel, IChannelHttpClient channelHttpClient,
            List<ItemDto> items, WorldConfiguration worldConfiguration, ISerializer serializer)
            : base(httpClientFactory, channel, channelHttpClient)
        {
            ApiUrl = "api/mail";
            RequireConnection = true;
            _items = items;
            _serializer = serializer;
            _worldConfiguration = worldConfiguration;
        }

        public void SendGift(ICharacterEntity characterEntity, long receiverId, short vnum, short amount, sbyte rare, byte upgrade, bool isNosmall)
        {
            var it = _items.Find(item => item.VNum == vnum);
            if (it == null)
            {
                return;
            }
            if (it.ItemType != ItemType.Weapon && it.ItemType != ItemType.Armor && it.ItemType != ItemType.Specialist)
            {
                upgrade = 0;
            }
            else if (it.ItemType != ItemType.Weapon && it.ItemType != ItemType.Armor)
            {
                rare = 0;
            }

            if (rare > 8 || rare < -2)
            {
                rare = 0;
            }
            if (upgrade > 10 && it.ItemType != ItemType.Specialist)
            {
                upgrade = 0;
            }
            else if (it.ItemType == ItemType.Specialist && upgrade > 15)
            {
                upgrade = 0;
            }

            if (amount > _worldConfiguration.MaxItemAmount)
            {
                amount = _worldConfiguration.MaxItemAmount;
            }
            if (amount == 0)
            {
                amount = 1;
            }
            amount = it.Type == NoscorePocketType.Etc || it.Type == NoscorePocketType.Main ? amount : (byte)1;
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

            Post<LanguageKey>(mail);

            //todo move to api
            //if (id != CharacterId)
            //{
            //    return;
            //}
            //Session.SendPacket(GenerateSay($"{Language.Instance.GetMessageFromKey("ITEM_GIFTED")} {mail.AttachmentAmount}", 12));
        }
    }
}
