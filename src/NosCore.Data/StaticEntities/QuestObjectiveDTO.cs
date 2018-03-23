using System.ComponentModel.DataAnnotations;

namespace NosCore.Data
{
    public class QuestObjectiveDTO : IDTO
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
