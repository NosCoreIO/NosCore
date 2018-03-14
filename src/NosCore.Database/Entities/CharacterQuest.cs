using NosCore.Database.Entities.Base;
using System.Collections.Generic;

namespace NosCore.Database.Entities
{
    public class CharacterQuest : SynchronizableBaseEntity
    {
        #region Properties

        public virtual Character Character { get; set; }

        public long CharacterId { get; set; }

        public virtual Quest Quest { get; set; }

        public short QuestId { get; set; }

        public int FirstObjective { get; set; }

        public int SecondObjective { get; set; }

        public int ThirdObjective { get; set; }

        public int FourthObjective { get; set; }

        public int FifthObjective { get; set; }

        public bool IsMainQuest { get; set; }

        #endregion
    }
}