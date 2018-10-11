using System;
using System.ComponentModel.DataAnnotations;
using NosCore.Shared.Enumerations.Items;

namespace NosCore.Data
{
    public class ItemInstanceDTO : IDTO
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public short Amount { get; set; }

        public long? BazaarItemId { get; set; }

        public long? BoundCharacterId { get; set; }

        public long CharacterId { get; set; }

        public short Design { get; set; }

        public int DurabilityPoint { get; set; }

        public DateTime? ItemDeleteTime { get; set; }

        public short ItemVNum { get; set; }

        public short Rare { get; set; }

        public short Slot { get; set; }

        public PocketType Type { get; set; }

        public byte Upgrade { get; set; }
    }
}
