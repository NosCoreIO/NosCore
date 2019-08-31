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
using ChickenAPI.Packets.Interfaces;
using ChickenAPI.Packets.ServerPackets.Parcel;
using UpdateStatActionType = NosCore.Data.Enumerations.UpdateStatActionType;

namespace NosCore.Data.WebApi
{
    public class MailData
    {
        public short Amount { get; set; }
        public string ReceiverName { get; set; }
        public string SenderName { get; set; }
        public short MailId { get; set; }
        public string Title { get; set; }
        public DateTime Date { get; set; }
        public short AttachmentVNum { get; set; }
        public short ItemType { get; set; }
        public bool IsSenderCopy { get; set; }

        public PostPacket GeneratePostMessage(byte type)
        {
            return new PostPacket
            {
                Type = 5,
                Unknown = type,
                Id = MailId,
            };
            //return $"post 5 {type} {MailList.First(s => s.Value == mailDto).Key} 0 0 {(byte)mailDto.SenderClass} {(byte)mailDto.SenderGender} {mailDto.SenderMorphId} {(byte)mailDto.SenderHairStyle} {(byte)mailDto.SenderHairColor} {mailDto.EqPacket} {sender.Name} {mailDto.Title} {mailDto.Message}";
        }

        public IPacket GeneratePost(byte type)
        {
            switch (type)
            {
                case 0:
                    return new ParcelPacket
                    {
                        Type = 1,
                        Unknown = 1,
                        Id = MailId,
                        ParcelAttachment = new ParcelAttachmentSubPacket
                        {
                            TitleType = Title == "NOSMALL" ? (byte)1 : (byte)4,
                            Unknown2 = 0,
                            Date = Date.ToString("yyMMddHHmm"),
                            Title = Title,
                            AttachmentVNum = AttachmentVNum,
                            AttachmentAmount = Amount,
                            ItemType = ItemType
                        }
                    };
                case 1:
                case 2:
                    //return $"post 1 {type} {MailList.First(s => s.Value.MailId == mail.MailId).Key} 0 {(mail.IsOpened ? 1 : 0)} {mail.Date.ToString("yyMMddHHmm")} {(type == 2 ? DAOFactory.CharacterDAO.FirstOrDefault(s => s.CharacterId == mail.ReceiverId).Name : DAOFactory.CharacterDAO.FirstOrDefault(s => s.CharacterId == mail.SenderId).Name)} {mail.Title}";
                    return new PostPacket
                    {
                        Type = 1,
                        Unknown = type,
                        Id = MailId,
                    };
                default:
                    throw new ArgumentException();
            }
        }
    }
}