using System;

namespace OpenNosCore.Database.Entities
{
    public class MinilandObject
    {
        #region Properties

        public virtual Character Character { get; set; }

        public long CharacterId { get; set; }

        public virtual ItemInstance ItemInstance { get; set; }

        public Guid? ItemInstanceId { get; set; }

        public byte Level1BoxAmount { get; set; }

        public byte Level2BoxAmount { get; set; }

        public byte Level3BoxAmount { get; set; }

        public byte Level4BoxAmount { get; set; }

        public byte Level5BoxAmount { get; set; }

        public short MapX { get; set; }

        public short MapY { get; set; }

        public long MinilandObjectId { get; set; }

        #endregion
    }
}