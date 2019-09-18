using NosCore.Data.Enumerations.Buff;
using System;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Data
{
    public class StaticBonusDto : IDto
    {
        public long CharacterId { get; set; }

        public DateTime DateEnd { get; set; }

        [Key]
        public long StaticBonusId { get; set; }

        public StaticBonusType StaticBonusType { get; set; }
    }
}
