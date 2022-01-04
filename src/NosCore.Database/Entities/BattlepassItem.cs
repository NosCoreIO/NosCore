using NosCore.Database.Entities.Base;

namespace NosCore.Database.Entities
{
    public class BattlepassItem : SynchronizableBaseEntity
    {
        public virtual Item Item { get; set; } = null!;

        public short VNum { get; set; }

        public short Amount { get; set; }

        public bool IsSuperReward { get; set; }

        public bool IsPremium { get; set; }

        public long BearingId { get; set; }
    }
}
