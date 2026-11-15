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

        public MailBoxType BoxOf(string? characterName) =>
            !MailDto.IsSenderCopy && ReceiverName == characterName ? MailBoxType.Received : MailBoxType.Sent;

        public PostPacket GeneratePostMessage(MailBoxType box)
        {
            return new PostPacket
            {
                Type = PostPacketType.Opened,
                PostType = box,
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

        public IPacket GeneratePost(MailBoxType box)
        {
            if (ItemInstance != null)
            {
                return new ParcelPacket
                {
                    Type = ParcelPacketType.Listed,
                    Box = box,
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
            }

            return new PostPacket
            {
                Type = PostPacketType.Listed,
                PostType = box,
                Id = (short)MailId,
                Unknown = 0,
                IsOpened = MailDto.IsOpened,
                DateTime = MailDto.Date.ToString("yyMMddHHmm", new DateTimeFormatInfo()),
                SenderName = box == MailBoxType.Sent ? ReceiverName : SenderName,
                Title = MailDto.Title,
                Message = MailDto.Message
            };
        }

        public Guid Id { get; set; } = Guid.NewGuid();
    }
}
