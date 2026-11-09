//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Data.Enumerations.Bazaar;
using NosCore.Database.Entities.Base;
using System;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    public class BazaarOperation : IEntity
    {
        [Key]
        public Guid BazaarOperationId { get; set; }

        public long BazaarItemId { get; set; }

        public long CharacterId { get; set; }

        public BazaarOperationType Type { get; set; }

        public BazaarOperationState State { get; set; }

        public short Amount { get; set; }

        public short SoldAmount { get; set; }

        public long Price { get; set; }

        public long Gold { get; set; }

        public long Taxes { get; set; }

        public Instant CreatedAt { get; set; }
    }
}
