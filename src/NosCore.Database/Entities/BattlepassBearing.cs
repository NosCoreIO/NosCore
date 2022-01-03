using NosCore.Database.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    public class BattlepassBearing : IEntity
    {
        [Key]
        public long Id { get; set; }

        public int MinimumBattlePassPoint { get; set; }

        public int MaximumBattlePassPoint { get; set; }
    }
}
