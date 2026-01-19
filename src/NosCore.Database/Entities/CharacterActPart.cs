//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Database.Entities.Base;

namespace NosCore.Database.Entities
{
    public class CharacterActPart : SynchronizableBaseEntity
    {
        public long CharacterId { get; set; }
        public virtual Character Character { get; set; } = null!;

        public byte ActPartId { get; set; }
        public virtual ActPart ActPart { get; set; } = null!;

        public byte CurrentTs;
    }
}
