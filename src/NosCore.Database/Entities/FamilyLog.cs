using System;
using System.ComponentModel.DataAnnotations;
using NosCore.Shared.Enumerations.Family;

namespace NosCore.Database.Entities
{
    public class FamilyLog
    {
        #region Properties

        public virtual Family Family { get; set; }

        public long FamilyId { get; set; }

        [MaxLength(255)]
        public string FamilyLogData { get; set; }

        public long FamilyLogId { get; set; }

        public FamilyLogType FamilyLogType { get; set; }

        public DateTime Timestamp { get; set; }

        #endregion
    }
}