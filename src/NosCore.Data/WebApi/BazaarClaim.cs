//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;

namespace NosCore.Data.WebApi
{
    // What the seller's channel applies once the master accepted a collection: the gold for the
    // settled sales and, when some of the listing is unsold, that rest as Item.
    public class BazaarClaim
    {
        public BazaarItemDto Listing { get; set; } = null!;

        public ItemInstanceDto? Item { get; set; }

        public short SoldAmount { get; set; }

        public short ReturnedAmount { get; set; }

        public long Gold { get; set; }

        public long Taxes { get; set; }
    }
}
