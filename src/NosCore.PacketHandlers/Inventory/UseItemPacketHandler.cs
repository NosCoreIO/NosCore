//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Inventory;
using System.Threading.Tasks;
using Wolverine;

namespace NosCore.PacketHandlers.Inventory
{
    public class UseItemPacketHandler(IMessageBus messageBus) : PacketHandler<UseItemPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(UseItemPacket useItemPacket, ClientSession clientSession)
        {
            var inv = clientSession.Character.InventoryService.LoadBySlotAndType(
                useItemPacket.Slot, (NoscorePocketType)useItemPacket.Type);
            if (inv?.ItemInstance?.Item == null)
            {
                return Task.CompletedTask;
            }

            return messageBus.PublishAsync(new ItemUsedEvent(clientSession, inv, useItemPacket)).AsTask();
        }
    }
}
