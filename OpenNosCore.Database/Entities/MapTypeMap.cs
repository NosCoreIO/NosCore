
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenNosCore.Database.Entities
{
    public class MapTypeMap
    {
        #region Properties

        public short MapTypeMapId { get; set; }

        public virtual Map Map { get; set; }

        public short MapId { get; set; }

        public virtual MapType MapType { get; set; }

        public short MapTypeId { get; set; }

        #endregion
    }
}