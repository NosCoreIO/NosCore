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

using System;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.Interfaces;
using ChickenAPI.Packets.ServerPackets.Parcel;
using ChickenAPI.Packets.ServerPackets.Visibility;
using NosCore.Data.Dto;

namespace NosCore.Data.WebApi
{
    public class MailData
    {
        public string ReceiverName { get; set; }
        public string SenderName { get; set; }
        public long MailId { get; set; }
        public MailDto MailDto { get; set; }
        public ItemInstanceDto ItemInstance { get; set; }
        public short ItemType { get; set; }

        public PostPacket GeneratePostMessage(int type)
        {
            return new PostPacket
            {
                Type = 5,
                PostType = (byte) type,
                Id = (short) MailId,
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

        public IPacket GeneratePost(byte type)
        {
            switch (type)
            {
                case 3:
                case 0:
                    return new ParcelPacket
                    {
                        Type = 1,
                        Unknown = type == 0 ? (byte) 1 : (byte) 2,
                        Id = (short) MailId,
                        ParcelAttachment = new ParcelAttachmentSubPacket
                        {
                            TitleType = MailDto.Title == "NOSMALL" ? (byte) 1 : (byte) 4,
                            Unknown2 = 0,
                            Date = MailDto.Date.ToString("yyMMddHHmm"),
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
                        Id = (short) MailId,
                        Unknown = 0,
                        IsOpened = MailDto.IsOpened,
                        DateTime = MailDto.Date.ToString("yyMMddHHmm"),
                        SenderName = type == 2 ? ReceiverName : SenderName,
                        Title = MailDto.Title,
                        Message = MailDto.Message
                    };
                default:
                    throw new ArgumentException();
            }
        }
    }
}