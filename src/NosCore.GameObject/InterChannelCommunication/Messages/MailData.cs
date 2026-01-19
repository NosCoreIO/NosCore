//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Parcel;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.Enumerations;
using System;
using System.Globalization;

namespace NosCore.GameObject.InterChannelCommunication.Messages
{
    public class MailData : IMessage
    {
        public string? ReceiverName { get; set; }
        public string? SenderName { get; set; }
        public long MailId { get; set; }
        public MailDto MailDto { get; set; } = new();
        public ItemInstanceDto? ItemInstance { get; set; }
        public short ItemType { get; set; }

        public PostPacket GeneratePostMessage(int type)
        {
            return new PostPacket
            {
                Type = 5,
                PostType = (byte)type,
                Id = (short)MailId,
                Unknown = 0,
                PostSubPacket = new PostSubPacket
                {
                    Class = MailDto.SenderCharacterClass ?? CharacterClassType.Adventurer,
                    Gender = MailDto.SenderGender ?? GenderType.Female,
                    MorphId = MailDto.SenderMorphId ?? 0,
                    HairStyle = MailDto.SenderHairStyle ?? HairStyleType.HairStyleA,
                    HairColor = MailDto.SenderHairColor ?? HairColorType.Black,
                    Equipment = new InEquipmentSubPacket
                    {
                        Armor = MailDto.Armor,
                        CostumeHat = MailDto.CostumeHat,
                        CostumeSuit = MailDto.CostumeSuit,
                        Fairy = MailDto.Fairy,
                        Hat = MailDto.Hat,
                        MainWeapon = MailDto.MainWeapon,
                        Mask = MailDto.Mask,
                        SecondaryWeapon = MailDto.SecondaryWeapon,
                        WeaponSkin = MailDto.WeaponSkin,
                        WingSkin = MailDto.WingSkin
                    }
                },
                SenderName = SenderName,
                Title = MailDto.Title,
                Message = MailDto.Message
            };
        }

        public IPacket? GeneratePost(byte type)
        {
            switch (type)
            {
                case 3:
                case 0:
                    return ItemInstance == null ? null : new ParcelPacket
                    {
                        Type = 1,
                        Unknown = type == 0 ? (byte)1 : (byte)2,
                        Id = (short)MailId,
                        ParcelAttachment = new ParcelAttachmentSubPacket
                        {
                            TitleType = MailDto.Title == "NOSMALL" ? (byte)1 : (byte)4,
                            Unknown2 = 0,
                            Date = MailDto.Date.ToString("yyMMddHHmm", new DateTimeFormatInfo()),
                            Title = MailDto.Title,
                            AttachmentVNum = ItemInstance.ItemVNum,
                            AttachmentAmount = ItemInstance.Amount,
                            ItemType = ItemType
                        }
                    };
                case 1:
                case 2:
                    return new PostPacket
                    {
                        Type = 1,
                        PostType = type,
                        Id = (short)MailId,
                        Unknown = 0,
                        IsOpened = MailDto.IsOpened,
                        DateTime = MailDto.Date.ToString("yyMMddHHmm", new DateTimeFormatInfo()),
                        SenderName = type == 2 ? ReceiverName : SenderName,
                        Title = MailDto.Title,
                        Message = MailDto.Message
                    };
                default:
                    throw new ArgumentException();
            }
        }

        public Guid Id { get; set; } = Guid.NewGuid();
    }
}
