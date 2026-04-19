//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Data.Dto;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;

namespace NosCore.GameObject.Services.MailService;

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
