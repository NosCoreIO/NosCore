using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace OpenNosCore.Database.Entities
{
    public class QuestObjective
    {
        [Key]
        public short QuestObjectiveId { get; set; }

        public int Data { get; set; }

        public int Objective { get; set; }

        public int? SpecialData { get; set; }

        public short QuestId { get; set; }

        public virtual Quest Quest { get; set; }
    }
}
