using NosCore.Data;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using System.Reactive.Subjects;

namespace NosCore.GameObject.Services.ItemBuilder.Item
{
    public interface IItemInstance : IItemInstanceDto, IRequestableEntity
    {
        object Clone();
        Item Item { get; set; }
    }
}
