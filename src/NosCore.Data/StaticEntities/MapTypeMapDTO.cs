using System.ComponentModel.DataAnnotations;

namespace NosCore.Data.StaticEntities
{
	public class MapTypeMapDTO : IDTO
	{
        #region Properties
        [Key]
        public object[] Key
        {
            get
            {
                return new object[] { MapId, MapTypeId };
            }
        }
        public short MapId { get; set; }
        public short MapTypeId { get; set; }

        #endregion
    }
}