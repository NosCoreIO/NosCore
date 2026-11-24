//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Database.Entities.Base;

namespace NosCore.Database.Entities
{
    public class TimeSpaceRecord : SynchronizableBaseEntity
    {
        public virtual Character Character { get; set; }

        public long CharacterId { get; set; }

        public short TimeSpaceId { get; set; }

        public int Score { get; set; }
    }
}
