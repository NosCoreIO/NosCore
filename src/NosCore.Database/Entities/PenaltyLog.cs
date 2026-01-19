//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Database.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    public class PenaltyLog : IEntity
    {
        public virtual Account Account { get; set; } = null!;

        public long AccountId { get; set; }

        [Required]
        public required string AdminName { get; set; }

        public Instant DateEnd { get; set; }

        public Instant DateStart { get; set; }

        public PenaltyType Penalty { get; set; }

        [Key]
        public int PenaltyLogId { get; set; }

        [Required]
        [MaxLength(255)]
        public required string Reason { get; set; }
    }
}
