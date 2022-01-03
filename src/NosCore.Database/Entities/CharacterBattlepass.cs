using NosCore.Database.Entities.Base;

namespace NosCore.Database.Entities
{
    public class CharacterBattlepass : SynchronizableBaseEntity
    {
        public long CharacterId { get; set; }

        public long Data { get; set; }

        public long? Data2 { get; set; }

        public bool? Data3 { get; set; }

        public bool IsItem { get; set; }
    }
}