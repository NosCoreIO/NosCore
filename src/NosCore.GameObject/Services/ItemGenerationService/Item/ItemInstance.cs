//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;
using System;

namespace NosCore.GameObject.Services.ItemGenerationService.Item
{
    public class ItemInstance : ItemInstanceDto, IItemInstance
    {
        public ItemInstance(Item item)
        {
            Id = Guid.NewGuid();
            Item = item;
            ItemVNum = item.VNum;
        }

        [Obsolete]
        public ItemInstance()
        {
        }

        public Item Item { get; set; } = null!;

        public object Clone()
        {
            return (ItemInstance)MemberwiseClone();
        }
    }
}
