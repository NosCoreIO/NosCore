//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Data.Enumerations.Audit;
using System;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    public class AuditLog
    {
        [Key]
        public Guid AuditId { get; set; }

        [Required]
        [MaxLength(80)]
        public required string TargetId { get; set; }

        [Required]
        [MaxLength(32)]
        public required string TargetType { get; set; }

        public Instant Time { get; set; }

        public AuditLogType AuditLogType { get; set; }

        [Required]
        [MaxLength(255)]
        public required string Value { get; set; }
    }
}
