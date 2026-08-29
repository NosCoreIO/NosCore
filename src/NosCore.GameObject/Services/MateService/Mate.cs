//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;

namespace NosCore.GameObject.Services.MateService
{
    public class Mate : MateDto
    {
        public NpcMonsterDto NpcMonster { get; set; } = null!;

        public long MateTransportId { get; set; }

        public byte PetSlot { get; set; }

        /// <summary>Where the mate is now; MapX/MapY are where it was saved.</summary>
        public short PositionX { get; set; }

        /// <inheritdoc cref="PositionX" />
        public short PositionY { get; set; }

        /// <summary>The mate's ECS entity while it is out, null while it is not.</summary>
        public Ecs.MateComponentBundle? Entity { get; set; }

        public int MaxHp => NpcMonster.MaxHp;

        public int MaxMp => NpcMonster.MaxMp;

        /// <summary>Set on load; the curve lives in NosCore.Algorithm.</summary>
        public long XpLoad { get; set; }
    }
}
