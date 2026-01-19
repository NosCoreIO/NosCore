//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Database.Entities.Base;
using System;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    public class BazaarItem : IEntity
    {
        public virtual Character Seller { get; set; } = null!;

        public short Amount { get; set; }

        [Key]
        public long BazaarItemId { get; set; }

        public Instant DateStart { get; set; }

        public short Duration { get; set; }

        public bool IsPackage { get; set; }

        public virtual ItemInstance ItemInstance { get; set; } = null!;

        public Guid ItemInstanceId { get; set; }

        public bool MedalUsed { get; set; }

        public long Price { get; set; }

        public long SellerId { get; set; }
    }
}
