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
using NosCore.GameObject.Ecs.Systems;

namespace NosCore.GameObject.Helper;

public static class GiftHelper
{
    public static MailRequest GenerateMailRequest(IClock clock, PlayerContext player, long receiverId,
        IItemInstanceDto? itemInstance,
        short? vnum, short? amount, sbyte? rare,
        byte? upgrade, bool isNosmall, string? title, string? text, ICharacterPacketSystem characterPacketSystem)
    {
        var equipment = isNosmall ? null : characterPacketSystem.GetEquipmentSubPacket(player);
        var mail = new MailDto
        {
            IsOpened = false,
            Date = clock.GetCurrentInstant(),
            ReceiverId = receiverId,
            IsSenderCopy = false,
            ItemInstanceId = itemInstance?.Id,
            Title = isNosmall ? "NOSMALL" : title ?? player.Name,
            Message = text,
            SenderId = isNosmall ? null : player.VisualId,
            SenderCharacterClass = isNosmall ? null : player.Class,
            SenderGender = isNosmall ? null : player.Gender,
            SenderHairColor = isNosmall ? null : player.HairColor,
            SenderHairStyle = isNosmall ? null : player.HairStyle,
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
            SenderMorphId = isNosmall ? null : player.Morph == 0 ? (short)-1 : player.Morph
        };
        return new MailRequest { Mail = mail, VNum = vnum, Amount = amount, Rare = rare, Upgrade = upgrade };
    }
}