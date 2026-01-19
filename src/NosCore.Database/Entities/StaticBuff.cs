//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Database.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    public class StaticBuff : IEntity
    {
        public virtual Character Character { get; set; } = null!;

        public long CharacterId { get; set; }

        public virtual Card Card { get; set; } = null!;

        public short CardId { get; set; }

        public int RemainingTime { get; set; }

        [Key]
        public long StaticBuffId { get; set; }
    }
}
