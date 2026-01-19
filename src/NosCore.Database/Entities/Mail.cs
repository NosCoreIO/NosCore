//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Database.Entities.Base;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using System;
using System.ComponentModel.DataAnnotations;

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

        public Instant Date { get; set; }

        public bool IsOpened { get; set; }

        public bool IsSenderCopy { get; set; }

        public virtual ItemInstance? ItemInstance { get; set; }

        public Guid? ItemInstanceId { get; set; }

        [Key]
        public long MailId { get; set; }

        [Required]
        [MaxLength(255)]
        public required string Message { get; set; }

        public virtual Character Receiver { get; set; } = null!;

        public long ReceiverId { get; set; }

        public virtual Character? Sender { get; set; }

        public long? SenderId { get; set; }

        public CharacterClassType? SenderCharacterClass { get; set; }

        public GenderType? SenderGender { get; set; }

        public HairColorType? SenderHairColor { get; set; }

        public HairStyleType? SenderHairStyle { get; set; }

        public short? SenderMorphId { get; set; }

        [Required]
        [MaxLength(255)]
        public required string Title { get; set; }
    }
}
