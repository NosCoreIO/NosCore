namespace OpenNosCore.Database.Entities
{
    public class Respawn
    {
        #region Properties

        public virtual Character Character { get; set; }

        public long CharacterId { get; set; }

        public virtual Map Map { get; set; }

        public short MapId { get; set; }

        public long RespawnId { get; set; }

        public virtual RespawnMapType RespawnMapType { get; set; }

        public long RespawnMapTypeId { get; set; }

        public short X { get; set; }

        public short Y { get; set; }

        #endregion
    }
}