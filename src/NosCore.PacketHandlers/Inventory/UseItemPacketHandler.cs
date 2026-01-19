//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.Packets.ClientPackets.Inventory;
using System;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Inventory
{
    public class UseItemPacketHandler : PacketHandler<UseItemPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(UseItemPacket useItemPacket, ClientSession clientSession)
        {
            var inv =
                clientSession.Character.InventoryService.LoadBySlotAndType(useItemPacket.Slot,
                    (NoscorePocketType)useItemPacket.Type);

            inv?.ItemInstance?.Item?.Requests[typeof(IUseItemEventHandler)]?.OnNext(new RequestData<Tuple<InventoryItemInstance, UseItemPacket>>(clientSession,
                new Tuple<InventoryItemInstance, UseItemPacket>(inv, useItemPacket)));

            return inv?.ItemInstance?.Item?.Requests == null ? Task.CompletedTask : Task.WhenAll(inv.ItemInstance.Item.HandlerTasks);
        }
    }
}
