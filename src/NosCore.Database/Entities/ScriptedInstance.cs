//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Interaction;
using NosCore.Database.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    public class ScriptedInstance : IEntity
    {
        public virtual Map Map { get; set; } = null!;

        public short MapId { get; set; }

        public short PositionX { get; set; }

        public short PositionY { get; set; }

        public string? Label { get; set; }

        [MaxLength(int.MaxValue)]
        public string? Script { get; set; }

        [Key]
        public short ScriptedInstanceId { get; set; }

        public ScriptedInstanceType Type { get; set; }
    }
}
