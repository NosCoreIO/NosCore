using OpenNosCore.Domain.Character;

namespace OpenNosCore.Database.Entities
{
    public class CharacterRelation
    {
        #region Properties

        public virtual Character Character1 { get; set; }

        public virtual Character Character2 { get; set; }

        public long CharacterId { get; set; }

        public long CharacterRelationId { get; set; }

        public long RelatedCharacterId { get; set; }

        public CharacterRelationType RelationType { get; set; }

        #endregion
    }
}