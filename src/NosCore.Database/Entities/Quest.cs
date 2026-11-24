//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.DataAttributes;
using NosCore.Data.Enumerations.I18N;
using NosCore.Database.Entities.Base;
using NosCore.Packets.Enumerations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NosCore.Database.Entities
{
    [StaticMetaData(LoadedMessage = LogLanguageKey.QUESTS_LOADED)]
    public class Quest : IStaticEntity
    {
        public Quest()
        {
            QuestObjective = new HashSet<QuestObjective>();
            CharacterQuest = new HashSet<CharacterQuest>();
            QuestQuestReward = new HashSet<QuestQuestReward>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public short QuestId { get; set; }

        public QuestType QuestType { get; set; }

        public virtual ICollection<CharacterQuest> CharacterQuest { get; set; }

        public virtual ICollection<QuestQuestReward> QuestQuestReward { get; set; }

        public byte LevelMin { get; set; }

        public byte LevelMax { get; set; }

        public int? StartDialogId { get; set; }

        public int? EndDialogId { get; set; }

        // The NPC that answers with InterimDialogId while the quest is held and its objectives are not met.
        public short? InterimNpcVNum { get; set; }

        public int? InterimDialogId { get; set; }

        public virtual ICollection<QuestObjective> QuestObjective { get; set; }

        public short? TargetMap { get; set; }

        public short? TargetX { get; set; }

        public short? TargetY { get; set; }

        //this would create circular reference if it was FK
        public short? NextQuestId { get; set; }

        // The first quest of the LINK chain this quest belongs to.
        public short QuestLineId { get; set; }

        public bool IsDaily { get; set; }

        public bool AutoFinish { get; set; }

        public bool IsSecondary { get; set; }

        public int? SpecialData { get; set; }

        //this would create circular reference if it was FK
        public short? RequiredQuestId { get; set; }

        public NrunRunnerType? OfferedBy { get; set; }

        // The argument of the OfferedBy button that names this quest, when the NPC has one button per quest.
        public short? OfferOption { get; set; }

        [Required]
        [MaxLength(255)]
        [I18NString(typeof(I18NQuest))]
        public required string Title { get; set; }

        [Required]
        [MaxLength(255)]
        [I18NString(typeof(I18NQuest))]
        public required string Desc { get; set; }
    }
}
