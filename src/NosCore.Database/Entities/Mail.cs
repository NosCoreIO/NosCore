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
using System.ComponentModel.DataAnnotations;
using NosCore.Packets.Enumerations;
using JetBrains.Annotations;
using NosCore.Database.Entities.Base;

namespace NosCore.Database.Entities
{
    public class Mail : IEntity
    {
        public short? Hat { get; set; }

        public short? Armor { get; set; }

        public short? MainWeapon { get; set; }

        public short? SecondaryWeapon { get; set; }

        public short? Mask { get; set; }

        public short? Fairy { get; set; }

        public short? CostumeSuit { get; set; }

        public short? CostumeHat { get; set; }

        public short? WeaponSkin { get; set; }

        public short? WingSkin { get; set; }

        public DateTime Date { get; set; }

        public bool IsOpened { get; set; }

        public bool IsSenderCopy { get; set; }

        [CanBeNull]
        public virtual ItemInstance ItemInstance { get; set; } = null!;

        public Guid? ItemInstanceId { get; set; }

        [Key]
        public long MailId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Message { get; set; } = "";

        public virtual Character Receiver { get; set; } = null!;

        public long ReceiverId { get; set; }

        public virtual Character Sender { get; set; } = null!;

        public long? SenderId { get; set; }

        public CharacterClassType? SenderCharacterClass { get; set; }

        public GenderType? SenderGender { get; set; }

        public HairColorType? SenderHairColor { get; set; }

        public HairStyleType? SenderHairStyle { get; set; }

        public short? SenderMorphId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = "";
    }
}