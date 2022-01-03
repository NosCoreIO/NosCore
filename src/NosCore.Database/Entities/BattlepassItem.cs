using NosCore.Database.Entities.Base;

namespace NosCore.Database.Entities
{
    public class BattlepassItem : SynchronizableBaseEntity
    {
        public short ItemVNum { get; set; }

        public short Amount { get; set; }

        public bool IsSuperReward { get; set; }

        public bool IsPremium { get; set; }

        public long BearingId { get; set; }
    }
}
