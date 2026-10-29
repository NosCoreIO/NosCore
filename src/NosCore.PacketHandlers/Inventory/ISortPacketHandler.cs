//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;

namespace NosCore.PacketHandlers.Inventory
{
    public class ISortPacketHandler : PacketHandler<ISortPacket>, IWorldPacketHandler
    {
        private static readonly HashSet<PocketType> SortablePockets =
            [PocketType.Equipment, PocketType.Main, PocketType.Etc];

        public override Task ExecuteAsync(ISortPacket packet, ClientSession clientSession)
        {
            if (!SortablePockets.Contains(packet.PocketType) || clientSession.Character.InExchangeOrShop)
            {
                return Task.CompletedTask;
            }

            var inventory = clientSession.Character.InventoryService;
            var slots = inventory.SortPocket((NoscorePocketType)packet.PocketType, clientSession.Account.Language);
            if (slots.Count == 0)
            {
                return Task.CompletedTask;
            }

            return clientSession.SendPacketAsync(new IvnPacket
            {
                Type = packet.PocketType,
                IvnSubPackets = slots.Select(slot =>
                    inventory.LoadBySlotAndType(slot, (NoscorePocketType)packet.PocketType)?.ItemInstance
                        .GenerateIvnSubPacket(packet.PocketType, slot)
                    ?? ((IItemInstance?)null).GenerateIvnSubPacket(packet.PocketType, slot)).ToList()
            });
        }
    }
}
