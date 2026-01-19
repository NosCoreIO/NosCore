//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using System;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.ItemGenerationService.Handlers
{
    public abstract class UseItemEventHandlerTestsBase
    {
        protected IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>? Handler;
        protected ClientSession? Session;
        protected readonly UseItemPacket UseItem = new();

        protected Task ExecuteInventoryItemInstanceEventHandlerAsync(InventoryItemInstance item)
        {
            return Handler!.ExecuteAsync(
                new RequestData<Tuple<InventoryItemInstance, UseItemPacket>>(
                    Session!,
                    new Tuple<InventoryItemInstance, UseItemPacket>(
                        item, UseItem
                    )));
        }
    }
}
