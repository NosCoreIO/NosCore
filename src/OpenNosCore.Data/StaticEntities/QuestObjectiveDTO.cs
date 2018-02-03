
using OpenNosCore.Database;
using System.ComponentModel.DataAnnotations;

namespace OpenNosCore.Data
{
    public class QuestObjectiveDTO : IDatabaseObject
    {
        public void Initialize()
        {

        }

        [Key]
        public short QuestObjectiveId { get; set; }

        public int Data { get; set; }

        public int Objective { get; set; }

        public int? SpecialData { get; set; }

        public short QuestId { get; set; }
    }
}
