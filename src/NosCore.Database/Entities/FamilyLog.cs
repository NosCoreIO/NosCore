//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Data.Enumerations.Family;
using NosCore.Database.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    public class FamilyLog : IEntity
    {
        public virtual Family Family { get; set; } = null!;

        public long FamilyId { get; set; }

        [MaxLength(255)]
        public string? FamilyLogData { get; set; }

        [Key]
        public long FamilyLogId { get; set; }

        public FamilyLogType FamilyLogType { get; set; }

        public Instant Timestamp { get; set; }
    }
}
