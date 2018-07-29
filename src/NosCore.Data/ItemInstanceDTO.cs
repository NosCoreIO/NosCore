using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using NosCore.Shared.Enumerations.Buff;
using NosCore.Shared.Enumerations.Items;

namespace NosCore.Data
{
    public class ItemInstanceDTO
    {
        [Key]
        public Guid Id { get; set; }

        public int Amount { get; set; }

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
