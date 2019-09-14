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

namespace NosCore.Data.WebApi
{
    public class MailData
    {
        public string ReceiverName { get; set; }
        public string SenderName { get; set; }
        public long MailId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public DateTime Date { get; set; }
        public ItemInstanceDto ItemInstance { get; set; }
        public short ItemType { get; set; }
        public bool IsSenderCopy { get; set; }
        public long MailDbKey { get; set; }
        public bool IsOpened { get; set; }

        public PostPacket GeneratePostMessage(byte type)
        {
            return new PostPacket
            {
                Type = 5,
                Unknown = type,
                Id = (short)MailId,
            };
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
                        Id = (short)MailId,
                        ParcelAttachment = new ParcelAttachmentSubPacket
                        {
                            TitleType = Title == "NOSMALL" ? (byte)1 : (byte)4,
                            Unknown2 = 0,
                            Date = Date.ToString("yyMMddHHmm"),
                            Title = Title,
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
                        IsOpened = IsOpened,
                        DateTime = Date.ToString("yyMMddHHmm"),
                        SenderName = type == 2 ? ReceiverName : SenderName,
                        Title = Title,
                        Message = Message,
                    };
                default:
                    throw new ArgumentException();
            }
        }
    }
}