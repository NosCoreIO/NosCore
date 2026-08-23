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

        /// <summary>
        /// Where the mate is standing right now, which is not where it was stored. MapX and MapY
        /// are the square it was last saved on; these two move with the owner.
        /// </summary>
        public short PositionX { get; set; }

        /// <inheritdoc cref="PositionX" />
        public short PositionY { get; set; }

        /// <summary>
        /// The mate's place in the world while it is out, or null while it is not.
        /// </summary>
        /// <remarks>
        /// A mate that can be hit has to be an entity like any other combatant — the battle
        /// service asks for an Arch handle, and giving mates a second notion of "thing that
        /// fights" would mean maintaining two. The handle lives here rather than in a registry
        /// because the mate is already the thing everyone holds.
        /// </remarks>
        public Ecs.MateComponentBundle? Entity { get; set; }

        public int MaxHp => NpcMonster.MaxHp;

        public int MaxMp => NpcMonster.MaxMp;

        /// <summary>
        /// Written when the mate is loaded rather than computed here: the curve lives in
        /// NosCore.Algorithm, and a data object has no business resolving a service.
        /// </summary>
        public long XpLoad { get; set; }
    }
}
