using OpenNosCore.Database;
using System.ComponentModel.DataAnnotations;


namespace OpenNosCore.Data
{
    public class MapDTO : IDatabaseObject
    {
        [Key]
        public short MapId { get; set; }

        [MaxLength(255)]
        public string Name { get; set; }

        public byte[] Data { get; set; }

        public int Music { get; set; }

        public bool ShopAllowed { get; set; }

        public void Initialize()
        {
        }
    }
}
