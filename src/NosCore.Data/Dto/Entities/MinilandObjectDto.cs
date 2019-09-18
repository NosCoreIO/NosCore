using System;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Data
{
    public class MinilandObjectDto : IDto
    {
        public Guid? InventoryItemInstanceId { get; set; }

        public byte Level1BoxAmount { get; set; }

        public byte Level2BoxAmount { get; set; }

        public byte Level3BoxAmount { get; set; }

        public byte Level4BoxAmount { get; set; }

        public byte Level5BoxAmount { get; set; }

        public short MapX { get; set; }

        public short MapY { get; set; }

        [Key]
        public Guid MinilandObjectId { get; set; }
    }
}
