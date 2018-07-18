namespace NosCore.Database.Entities
{
    public class MapTypeMap
    {
        public short MapTypeMapId { get; set; }

        public virtual Map Map { get; set; }

        public short MapId { get; set; }

        public virtual MapType MapType { get; set; }

        public short MapTypeId { get; set; }
    }
}