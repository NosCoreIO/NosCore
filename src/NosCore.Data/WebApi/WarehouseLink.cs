//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;

namespace NosCore.Data.WebApi
{
    public class WarehouseLink
    {
        public WarehouseDto? Warehouse { get; set; }

        public short Slot { get; set; }

        //todo move this to a generic ItemInstance with a converter
        public ItemInstanceDto? ItemInstance { get; set; }
    }
}
