using OpenNosCore.Database;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace OpenNosCore.Data
{
    public class QuestDTO : IDatabaseObject
    {

        public void Initialize()
        {

        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public short QuestId { get; set; }

        public int QuestType { get; set; }

        public byte LevelMin { get; set; }

        public byte LevelMax { get; set; }

        public int? StartDialogId { get; set; }

        public int? EndDialogId { get; set; }

        public ICollection<QuestObjectiveDTO> QuestObjective { get; set; }

        public ICollection<QuestRewardDTO> QuestRewards { get; set; }

        public short? TargetMap { get; set; }

        public short? TargetX { get; set; }

        public short? TargetY { get; set; }

        public int InfoId { get; set; }

        public long? NextQuestId { get; set; }

        public bool IsDaily { get; set; }

        public int? SpecialData { get; set; }
    }
}
