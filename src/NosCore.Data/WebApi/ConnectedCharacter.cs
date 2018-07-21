using System.Collections.Generic;

namespace NosCore.Data.WebApi
{
    public class ConnectedCharacter
    {
        public string Name { get; set; }

        public long Id { get; set; }

        public ICollection<CharacterRelation> Relations { get; set; }
    }
}