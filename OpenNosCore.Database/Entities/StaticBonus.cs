using OpenNosCore.Domain.Buff;
using System;

namespace OpenNosCore.Database.Entities
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