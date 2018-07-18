using NosCore.Shared.Enumerations.Interaction;

namespace NosCore.Database.Entities
{
    public class Teleporter
    {
        #region Properties

        public short Index { get; set; }

        public TeleporterType Type { get; set; }

        public virtual Map Map { get; set; }

        public short MapId { get; set; }

        public virtual MapNpc MapNpc { get; set; }

        public int MapNpcId { get; set; }

        public short MapX { get; set; }

        public short MapY { get; set; }

        public short TeleporterId { get; set; }

        #endregion
    }
}