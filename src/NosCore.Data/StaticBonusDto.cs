using System;
using System.ComponentModel.DataAnnotations;
using NosCore.Data.Enumerations.Buff;

namespace NosCore.Data
{
    public class StaticBonusDto
    {
        public long CharacterId { get; set; }

        public DateTime DateEnd { get; set; }

        [Key]
        public long StaticBonusId { get; set; }

        public StaticBonusType StaticBonusType { get; set; }
    }
}
