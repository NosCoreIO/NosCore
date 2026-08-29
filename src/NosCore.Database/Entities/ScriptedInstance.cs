//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.DataAttributes;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Database.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    [StaticMetaData(LoadedMessage = LogLanguageKey.TIMESPACES_PARSED)]
    public class ScriptedInstance : IStaticEntity
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

        public byte LevelMinimum { get; set; }

        public byte LevelMaximum { get; set; }

        public bool IsHeroic { get; set; }
    }
}
