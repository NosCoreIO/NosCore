//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Miniland;

namespace NosCore.Data.WebApi
{
    public class WareHouseDepositRequest
    {
        public long OwnerId { get; set; }
        public WarehouseType WarehouseType { get; set; }
        public ItemInstanceDto? ItemInstance { get; set; }
        public short Slot { get; set; }
    }
}
