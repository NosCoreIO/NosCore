//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;
using System;

namespace NosCore.GameObject.Services.ItemGenerationService.Item
{
    public class BoxInstance : BoxInstanceDto, IItemInstance
    {
        public BoxInstance(Item item)
        {
            Id = Guid.NewGuid();
            Item = item;
            ItemVNum = item.VNum;
        }


        public Item Item { get; set; }

        public object Clone()
        {
            return (BoxInstance)MemberwiseClone();
        }
    }
}
