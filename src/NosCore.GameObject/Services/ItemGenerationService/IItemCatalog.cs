//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;

namespace NosCore.GameObject.Services.ItemGenerationService
{
    public interface IItemCatalog
    {
        ItemDto? GetItem(short vnum);

        // For an effect only one item carries, such as gold or the savings book.
        ItemDto? GetItemWithEffect(ItemEffectType effect);

        // 0 when no item is that material, so a cost in it can never be paid.
        short GetMaterialVNum(MaterialType material);
    }
}
