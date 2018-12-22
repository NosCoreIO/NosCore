using NosCore.Data;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.Packets.ClientPackets;
using System;

namespace NosCore.GameObject.Services.ItemBuilder.Item
{
    public interface IItemInstance : IItemInstanceDto, IRequestableEntity<Tuple<IItemInstance, UseItemPacket>>
    {
        object Clone();
        Item Item { get; set; }
    }
}
