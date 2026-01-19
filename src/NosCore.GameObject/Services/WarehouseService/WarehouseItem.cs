//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Miniland;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.ItemStorage;
using System;

namespace NosCore.GameObject.Services.WarehouseService
{
    public class WarehouseItem : WarehouseItemDto, ISlotItem<WarehouseType>
    {
        public IItemInstance? ItemInstance { get; set; }

        public WarehouseType WarehouseType { get; set; }

        Guid ISlotItem.Id => Id;

        WarehouseType ISlotItem<WarehouseType>.SlotType
        {
            get => WarehouseType;
            set => WarehouseType = value;
        }
    }
}
