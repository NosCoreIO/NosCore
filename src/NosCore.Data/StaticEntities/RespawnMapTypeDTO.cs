using System.ComponentModel.DataAnnotations;

namespace NosCore.Data.StaticEntities
{
    public class RespawnMapTypeDTO : IDTO
    {
        public short DefaultMapId { get; set; }

        public short DefaultX { get; set; }

        public short DefaultY { get; set; }

        public string Name { get; set; }

        [Key]
        public long RespawnMapTypeId { get; set; }
    }
}