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

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NosCore.Packets.Enumerations;
using JetBrains.Annotations;
using Microsoft.AspNetCore.JsonPatch;
using NosCore.Core;
using NosCore.Core.HttpClients;
using NosCore.Core.HttpClients.ChannelHttpClients;
using NosCore.Data;
using NosCore.Data.Dto;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Interfaces;

namespace NosCore.GameObject.HttpClients.MailHttpClient
{
    public class MailHttpClient : MasterServerHttpClient, IMailHttpClient
    {
        public MailHttpClient(IHttpClientFactory httpClientFactory, Channel channel,
            IChannelHttpClient channelHttpClient)
            : base(httpClientFactory, channel, channelHttpClient)
        {
            ApiUrl = "api/mail";
            RequireConnection = true;
        }

        public Task SendGift(ICharacterEntity characterEntity, long receiverId, IItemInstanceDto itemInstance,
            bool isNosmall)
        {
            return Post<bool>(GenerateMailRequest(characterEntity, receiverId, itemInstance, null, null, null, null, isNosmall,
                null, null));
        }

        public Task SendGift(ICharacterEntity characterEntity, long receiverId, short vnum, short amount, sbyte rare,
            byte upgrade, bool isNosmall)
        {
            return Post<bool>(GenerateMailRequest(characterEntity, receiverId, null, vnum, amount, rare, upgrade, isNosmall,
                null, null));
        }

        public Task SendMessage(ICharacterEntity characterEntity, long receiverId, string title, string text)
        {
            return Post<bool>(GenerateMailRequest(characterEntity, receiverId, null, null, null, null, null, false, title,
                text));
        }

        public Task<IEnumerable<MailData>> GetGifts(long characterId)
        {
            return Get<IEnumerable<MailData>>($"-1&characterId={characterId}")!;
        }

        public async Task<MailData?> GetGift(long id, long characterId, bool isCopy)
        {
            return (await Get<IEnumerable<MailData>>($"{id}&characterId={characterId}&senderCopy={isCopy}")!.ConfigureAwait(false)).FirstOrDefault();
        }

        public Task DeleteGift(long giftId, long visualId, bool isCopy)
        {
            return Delete($"{giftId}&characterId={visualId}&senderCopy={isCopy}");
        }

        public Task ViewGift(long giftId, JsonPatchDocument<MailDto> mailData)
        {
            return Patch<MailData>(giftId, mailData);
        }

        private MailRequest GenerateMailRequest(ICharacterEntity characterEntity, long receiverId,
            IItemInstanceDto? itemInstance,
            short? vnum, short? amount, sbyte? rare,
            byte? upgrade, bool isNosmall, string? title, string? text)
        {
            var mail = new MailDto
            {
                IsOpened = false,
                Date = SystemTime.Now(),
                ReceiverId = receiverId,
                IsSenderCopy = false,
                ItemInstanceId = itemInstance?.Id,
                Title = isNosmall ? "NOSMALL" : title ?? characterEntity.Name,
                Message = text,
                SenderId = isNosmall ? (long?) null : characterEntity.VisualId,
                SenderCharacterClass = isNosmall ? (CharacterClassType?) null : characterEntity.Class,
                SenderGender = isNosmall ? (GenderType?) null : characterEntity.Gender,
                SenderHairColor = isNosmall ? (HairColorType?) null : characterEntity.HairColor,
                SenderHairStyle = isNosmall ? (HairStyleType?) null : characterEntity.HairStyle,
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
                SenderMorphId = isNosmall ? (short?) null : characterEntity.Morph == 0 ? (short) -1
                    : (short) (characterEntity.Morph > short.MaxValue ? 0 : characterEntity.Morph)
            };
            return new MailRequest {Mail = mail, VNum = vnum, Amount = amount, Rare = rare, Upgrade = upgrade};
        }
    }
}