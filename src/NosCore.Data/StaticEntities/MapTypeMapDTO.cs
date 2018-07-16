using System.ComponentModel.DataAnnotations;

namespace NosCore.Data.StaticEntities
{
	public class MapTypeMapDTO : IDTO
	{
        [Key]
        public short MapTypeMapId { get; set; }
        public short MapId { get; set; }
        public short MapTypeId { get; set; }
    }
}