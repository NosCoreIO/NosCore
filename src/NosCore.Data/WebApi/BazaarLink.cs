using NosCore.Data.Dto;

namespace NosCore.Data.WebApi
{
    public class BazaarLink
    {
        public BazaarItemDto BazaarItem { get; set; }

        //todo move this to a generic ItemInstance with a converter
        public ItemInstanceDto ItemInstance { get; set; }
        public string SellerName { get; set; }
    }
}