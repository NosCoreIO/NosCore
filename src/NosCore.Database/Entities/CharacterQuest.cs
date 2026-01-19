//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Database.Entities.Base;

namespace NosCore.Database.Entities
{
    public class CharacterQuest : SynchronizableBaseEntity
    {
        public virtual Character Character { get; set; } = null!;

        public long CharacterId { get; set; }

        public virtual Quest Quest { get; set; } = null!;

        public short QuestId { get; set; }

        public Instant? CompletedOn { get; set; }
    }
}
