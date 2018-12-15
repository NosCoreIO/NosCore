using NosCore.Data;

namespace NosCore.GameObject.Services.ItemBuilder.Item
{
    public interface IItemInstance : IItemInstanceDto
    {
        object Clone();
        Item Item { get; set; }
    }
}
