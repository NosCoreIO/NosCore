using System;
using System.ComponentModel.DataAnnotations;
using NosCore.Data.Enumerations.Audit;

namespace NosCore.Database.Entities
{
    public class AuditLog
    {
        [Key]
        public long AuditId { get; set; }
        
        public long TargetId { get; set; }
        
        [MaxLength(32)]
        public string TargetType { get; set; }
        
        public DateTime Time { get; set; }
        
        public AuditLogType AuditLogType { get; set; }
        
        [MaxLength(255)]
        public string Value { get; set; }
    }
}