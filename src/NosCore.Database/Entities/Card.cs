//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Database.Entities.Base;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static NosCore.Data.Enumerations.Buff.BCardType;

namespace NosCore.Database.Entities
{
    public class Card : IStaticEntity
    {
        public Card()
        {
            BCards = new HashSet<BCard>();
            StaticBuff = new HashSet<StaticBuff>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public short CardId { get; set; }

        public int Duration { get; set; }

        public int EffectId { get; set; }

        public byte Level { get; set; }

        [Required]
        [MaxLength(255)]
        [I18NString(typeof(I18NBCard))]
        public required string Name { get; set; }

        public int Delay { get; set; }

        public short TimeoutBuff { get; set; }

        public byte TimeoutBuffChance { get; set; }

        public CardType BuffType { get; set; }

        public byte Propability { get; set; }

        public virtual ICollection<BCard> BCards { get; set; }

        public virtual ICollection<StaticBuff> StaticBuff { get; set; }
    }
}
