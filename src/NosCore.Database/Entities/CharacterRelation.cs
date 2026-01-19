//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Database.Entities.Base;
using NosCore.Packets.Enumerations;
using System;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    public class CharacterRelation : IEntity
    {
        public virtual Character Character1 { get; set; } = null!;

        public virtual Character Character2 { get; set; } = null!;

        public long CharacterId { get; set; }

        [Key]
        public Guid CharacterRelationId { get; set; }

        public long RelatedCharacterId { get; set; }

        public CharacterRelationType RelationType { get; set; }
    }
}
