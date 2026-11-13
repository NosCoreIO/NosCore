//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.DataAttributes;
using NosCore.Data.Enumerations.Family;
using NosCore.Data.Enumerations.I18N;
using NosCore.Database.Entities.Base;
using NosCore.Packets.Enumerations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NosCore.Database.Entities
{
    [StaticMetaData(LoadedMessage = LogLanguageKey.FAMILY_QUESTS_LOADED)]
    public class FamilyQuest : IStaticEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public short QuestId { get; set; }

        /// <summary>The item whose name the family window shows for this quest.</summary>
        public short NameItemVNum { get; set; }

        public FamilyQuestKind Kind { get; set; }

        /// <summary>Null for quests counted by something NosCore does not track yet.</summary>
        public FamilyQuestObjective? Objective { get; set; }

        public byte FamilyLevelMinimum { get; set; }

        public int Target { get; set; }

        /// <summary>What the objective is about, such as the family level a level achievement waits for.</summary>
        public int? Parameter { get; set; }

        /// <summary>The achievement that has to be unlocked first.</summary>
        public short? PreviousQuestId { get; set; }

        public int Experience { get; set; }
    }
}
