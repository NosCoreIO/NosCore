using System.ComponentModel.DataAnnotations;
using NosCore.Shared.Enumerations.Character;

namespace NosCore.Data
{
    public class CharacterRelationDTO : IDTO
    {
        [Key]
        public long CharacterRelationId { get; set; }

        public long CharacterId { get; set; }

        public long RelatedCharacterId { get; set; }

        public CharacterRelationType RelationType { get; set; }
    }
}
