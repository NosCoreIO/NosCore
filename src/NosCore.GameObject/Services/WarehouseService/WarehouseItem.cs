//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Miniland;

namespace NosCore.GameObject.Services.WarehouseService
{
    public class WarehouseItem : WarehouseItemDto
    {
        public required IItemInstanceDto ItemInstance { get; set; }

        public WarehouseType WarehouseType { get; set; }
    }
}
