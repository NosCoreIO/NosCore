//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Database.Entities.Base;

namespace NosCore.Database.Entities
{
    public class MallDelivery : SynchronizableBaseEntity
    {
        public virtual Character Character { get; set; }

        public long CharacterId { get; set; }

        public short VNum { get; set; }

        public short Amount { get; set; }

        public Instant CreatedAt { get; set; }

        public Instant? DeliveredAt { get; set; }
    }
}
