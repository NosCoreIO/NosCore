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

namespace NosCore.Database.Entities
{
    [StaticMetaData(LoadedMessage = LogLanguageKey.QUESTREWARDS_LOADED)]
    public class QuestReward : IStaticEntity
    {
        public QuestReward()
        {
            QuestQuestReward = new HashSet<QuestQuestReward>();
        }
        [Key]
        public short QuestRewardId { get; set; }

        public QuestRewardType RewardType { get; set; }

        public int Data { get; set; }

        public int? SecondData { get; set; }

        public int? ThirdData { get; set; }

        public int? FourthData { get; set; }

        public byte Design { get; set; }

        public byte Rarity { get; set; }

        public byte Upgrade { get; set; }

        public int Amount { get; set; }

        public virtual ICollection<QuestQuestReward> QuestQuestReward { get; set; }
    }
}
