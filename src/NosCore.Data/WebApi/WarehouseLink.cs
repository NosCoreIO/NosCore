using NosCore.Data.Dto;

namespace NosCore.Data.WebApi
{
    public class WarehouseLink
    {
        public WarehouseDto Warehouse { get; set; }

        //todo move this to a generic ItemInstance with a converter
        public ItemInstanceDto ItemInstance { get; set; }
    }
}