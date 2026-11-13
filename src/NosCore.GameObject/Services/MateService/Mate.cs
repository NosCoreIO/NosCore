//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;

namespace NosCore.GameObject.Services.MateService
{
    public class Mate : MateDto
    {
        public required NpcMonsterDto NpcMonster { get; set; }

        public long MateTransportId { get; set; }

        public byte PetSlot { get; set; }

        /// <summary>The mate's ECS entity while it is out, null while it is not.</summary>
        public Ecs.MateComponentBundle? Entity { get; set; }

        /// <summary>
        /// The worn partner specialist's morph while the partner is transformed into it. Held here rather than
        /// on the entity, which is rebuilt on every map change.
        /// </summary>
        public short? SpecialistMorph { get; set; }

        public Instant SpecialistCooldownEndsAt { get; set; }

        /// <summary>Set while the mate is dead and waiting to come back to its owner.</summary>
        public Instant? RevivesAt { get; set; }

        /// <summary>Set on load and on level-up, like <see cref="MaxMp"/> and <see cref="XpLoad"/>.</summary>
        public int MaxHp { get; set; }

        public int MaxMp { get; set; }

        public long XpLoad { get; set; }

        /// <summary>Attack, defence and dodge at the mate's level, set with <see cref="MaxHp"/>.</summary>
        public short DamageMinimum { get; set; }

        /// <inheritdoc cref="DamageMinimum" />
        public short DamageMaximum { get; set; }

        /// <inheritdoc cref="DamageMinimum" />
        public short Concentrate { get; set; }

        /// <inheritdoc cref="DamageMinimum" />
        public short CloseDefence { get; set; }

        /// <inheritdoc cref="DamageMinimum" />
        public short DistanceDefence { get; set; }

        /// <inheritdoc cref="DamageMinimum" />
        public short MagicDefence { get; set; }

        /// <inheritdoc cref="DamageMinimum" />
        public short Dodge { get; set; }
    }
}
