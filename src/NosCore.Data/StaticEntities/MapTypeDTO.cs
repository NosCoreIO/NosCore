using System.ComponentModel.DataAnnotations;

namespace NosCore.Data.StaticEntities
{
	public class MapTypeDTO : IDTO
    {
        [Key]
        public short MapTypeId { get; set; }

        public string MapTypeName { get; set; }

        public short PotionDelay { get; set; }

        public long? RespawnMapTypeId { get; set; }

        public long? ReturnMapTypeId { get; set; }
    }
}