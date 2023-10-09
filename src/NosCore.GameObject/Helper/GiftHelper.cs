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

using NodaTime;
using NosCore.Data.Dto;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;

namespace NosCore.GameObject.Helper;

public static class GiftHelper
{
    public static MailRequest GenerateMailRequest(IClock clock, ICharacterEntity characterEntity, long receiverId,
        IItemInstanceDto? itemInstance,
        short? vnum, short? amount, sbyte? rare,
        byte? upgrade, bool isNosmall, string? title, string? text)
    {
        var equipment = isNosmall ? null : characterEntity.GetEquipmentSubPacket();
        var mail = new MailDto
        {
            IsOpened = false,
            Date = clock.GetCurrentInstant(),
            ReceiverId = receiverId,
            IsSenderCopy = false,
            ItemInstanceId = itemInstance?.Id,
            Title = isNosmall ? "NOSMALL" : title ?? characterEntity.Name,
            Message = text,
            SenderId = isNosmall ? null : characterEntity.VisualId,
            SenderCharacterClass = isNosmall ? null : characterEntity.Class,
            SenderGender = isNosmall ? null : characterEntity.Gender,
            SenderHairColor = isNosmall ? null : characterEntity.HairColor,
            SenderHairStyle = isNosmall ? null : characterEntity.HairStyle,
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
            SenderMorphId = isNosmall ? null : characterEntity.Morph == 0 ? (short)-1
                : characterEntity.Morph
        };
        return new MailRequest { Mail = mail, VNum = vnum, Amount = amount, Rare = rare, Upgrade = upgrade };
    }
}