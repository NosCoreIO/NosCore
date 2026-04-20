//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.DataAttributes;
using NosCore.Data.Enumerations.I18N;
using NosCore.Database.Entities.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    [StaticMetaData(LoadedMessage = LogLanguageKey.QUESTOBJECTIVES_LOADED)]
    public class QuestObjective : IStaticEntity
    {
        public QuestObjective()
        {
            CharacterQuestObjective = new HashSet<CharacterQuestObjective>();
        }

        [Key]
        public Guid QuestObjectiveId { get; set; }

        public int FirstData { get; set; }

        public int? SecondData { get; set; }

        public int? ThirdData { get; set; }

        public int? FourthData { get; set; }

        public short QuestId { get; set; }

        public virtual Quest Quest { get; set; } = null!;

        public virtual ICollection<CharacterQuestObjective> CharacterQuestObjective { get; set; }
    }
}
