using System;
using NosCore.Shared.Enumerations.Buff;

namespace NosCore.Database.Entities
{
    public class StaticBonus
    {
        #region Properties

        public virtual Character Character { get; set; }

        public long CharacterId { get; set; }

        public DateTime DateEnd { get; set; }

        public long StaticBonusId { get; set; }

        public StaticBonusType StaticBonusType { get; set; }

        #endregion
    }
}