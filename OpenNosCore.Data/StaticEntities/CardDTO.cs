using OpenNosCore.Database;
using OpenNosCore.Domain.Buff;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OpenNosCore.Data
{
    public class CardDTO : IDatabaseObject
    {
        [Key]
        public short CardId { get; set; }

        public int Duration { get; set; }

        public int EffectId { get; set; }

        public byte Level { get; set; }

        [MaxLength(255)]
        public string Name { get; set; }

        public int Delay { get; set; }

        public short TimeoutBuff { get; set; }

        public byte TimeoutBuffChance { get; set; }

        public BCardType.CardType BuffType { get; set; }

        public byte Propability { get; set; }

        public ICollection<BCardDTO> BCards { get; set; }
        
        public void Initialize()
        {
            
        }
    }
}
