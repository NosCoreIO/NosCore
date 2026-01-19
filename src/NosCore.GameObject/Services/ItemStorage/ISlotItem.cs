//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.ItemGenerationService.Item;
using System;

namespace NosCore.GameObject.Services.ItemStorage
{
    public interface ISlotItem
    {
        Guid Id { get; }
        short Slot { get; set; }
        IItemInstance? ItemInstance { get; }
    }

    public interface ISlotItem<TSlotType> : ISlotItem
        where TSlotType : struct, Enum
    {
        TSlotType SlotType { get; set; }
    }
}
