//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Database.Entities.Base;

namespace NosCore.Database.Entities
{
    public class QuicklistEntry : SynchronizableBaseEntity
    {
        public virtual Character Character { get; set; } = null!;

        public long CharacterId { get; set; }

        public short Morph { get; set; }

        public short IconVNum { get; set; }

        public short QuickListIndex { get; set; }

        public short Slot { get; set; }

        public short IconType { get; set; }

        public short Type { get; set; }
    }
}
