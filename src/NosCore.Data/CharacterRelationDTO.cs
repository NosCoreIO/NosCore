using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
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

        public virtual string CharacterName { get; set; }
    }
}
