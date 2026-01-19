//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.StaticEntities;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.ClientPackets.Inventory;
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ItemGenerationService.Item
{
    public class Item : ItemDto, IRequestableEntity<Tuple<InventoryItemInstance, UseItemPacket>>
    {
        public List<Task> HandlerTasks { get; set; } = new();

        public Dictionary<Type, Subject<RequestData<Tuple<InventoryItemInstance, UseItemPacket>>>> Requests { get; set; } = new()
        {
            [typeof(IUseItemEventHandler)] = new Subject<RequestData<Tuple<InventoryItemInstance, UseItemPacket>>>()
        };
    }
}
