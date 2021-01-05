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
using NosCore.Core;
using NosCore.Core.HttpClients;
using NosCore.Data.Dto;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace NosCore.GameObject.HttpClients.MailHttpClient
{
    public class MailHttpClient : MasterServerHttpClient, IMailHttpClient
    {
        public MailHttpClient(IHttpClientFactory httpClientFactory, Channel channel)
            : base(httpClientFactory, channel)
        {
            ApiUrl = "api/mail";
            RequireConnection = true;
        }

        public Task SendGiftAsync(ICharacterEntity characterEntity, long receiverId, IItemInstanceDto itemInstance,
            bool isNosmall)
        {
            return PostAsync<bool>(GenerateMailRequest(characterEntity, receiverId, itemInstance, null, null, null, null, isNosmall,
                null, null));
        }

        public Task SendGiftAsync(ICharacterEntity characterEntity, long receiverId, short vnum, short amount, sbyte rare,
            byte upgrade, bool isNosmall)
        {
            return PostAsync<bool>(GenerateMailRequest(characterEntity, receiverId, null, vnum, amount, rare, upgrade, isNosmall,
                null, null));
        }

        public Task SendMessageAsync(ICharacterEntity characterEntity, long receiverId, string title, string text)
        {
            return PostAsync<bool>(GenerateMailRequest(characterEntity, receiverId, null, null, null, null, null, false, title,
                text));
        }

        public Task<IEnumerable<MailData>> GetGiftsAsync(long characterId)
        {
            return GetAsync<IEnumerable<MailData>>($"-1&characterId={characterId}")!;
        }

        public async Task<MailData?> GetGiftAsync(long id, long characterId, bool isCopy)
        {
            return (await GetAsync<IEnumerable<MailData>>($"{id}&characterId={characterId}&senderCopy={isCopy}")!.ConfigureAwait(false)).FirstOrDefault();
        }

        public Task DeleteGiftAsync(long giftId, long visualId, bool isCopy)
        {
            return DeleteAsync($"{giftId}&characterId={visualId}&senderCopy={isCopy}");
        }

        public Task ViewGiftAsync(long giftId, JsonPatch mailData)
        {
            return PatchAsync<MailData>(giftId, mailData);
        }

        private MailRequest GenerateMailRequest(ICharacterEntity characterEntity, long receiverId,
            IItemInstanceDto? itemInstance,
            short? vnum, short? amount, sbyte? rare,
            byte? upgrade, bool isNosmall, string? title, string? text)
        {
            var equipment = isNosmall ? null : characterEntity.GetEquipmentSubPacket();
            var mail = new MailDto
            {
                IsOpened = false,
                Date = SystemTime.Now(),
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
                Hat = equipment?.Hat,
                Armor = equipment?.Armor,
                MainWeapon = equipment?.MainWeapon,
                SecondaryWeapon = equipment?.SecondaryWeapon,
                Mask = equipment?.Mask,
                Fairy = equipment?.Fairy,
                CostumeSuit = equipment?.CostumeSuit,
                CostumeHat = equipment?.CostumeHat,
                WeaponSkin = equipment?.WeaponSkin,
                WingSkin = equipment?.WingSkin,
                SenderMorphId = isNosmall ? (short?)null : characterEntity.Morph == 0 ? (short)-1
                    : characterEntity.Morph
            };
            return new MailRequest { Mail = mail, VNum = vnum, Amount = amount, Rare = rare, Upgrade = upgrade };
        }
    }
}