using NosCore.Data;
using NosCore.GameObject.Services.ItemBuilder.Item;

namespace NosCore.GameObject.Services.ItemBuilder
{
    public interface IItemBuilderService
    {
        ItemInstance Create(short itemToCreateVNum, long characterId, short amount = 1, sbyte rare = 0,
            byte upgrade = 0, byte design = 0);
        ItemInstance Convert(ItemInstanceDTO k);
    }
}
