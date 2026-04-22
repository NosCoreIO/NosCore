//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Database.Entities.Base;
using System;

namespace NosCore.Database.Entities
{
    public class CharacterQuestObjective : SynchronizableBaseEntity
    {
        public virtual CharacterQuest CharacterQuest { get; set; } = null!;

        public Guid CharacterQuestId { get; set; }

        public virtual QuestObjective QuestObjective { get; set; } = null!;

        public Guid QuestObjectiveId { get; set; }

        public int Count { get; set; }
    }
}
