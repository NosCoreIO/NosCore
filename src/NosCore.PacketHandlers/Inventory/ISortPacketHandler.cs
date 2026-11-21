//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.PacketHandlers.Inventory
{
    public class ISortPacketHandler : PacketHandler<ISortPacket>, IWorldPacketHandler
    {
        private static readonly HashSet<PocketType> SortablePockets =
            [PocketType.Equipment, PocketType.Main, PocketType.Etc];

        public override async Task ExecuteAsync(ISortPacket packet, ClientSession clientSession)
        {
            if (!SortablePockets.Contains(packet.PocketType))
            {
                return;
            }

            if (packet.Confirmed == null)
            {
                await clientSession.SendPacketAsync(new QnaiPacket
                {
                    YesPacket = new ISortPacket { PocketType = packet.PocketType, Confirmed = true },
                    Question = Game18NConstString.SortInventoryQuestion
                });
                return;
            }

            if (packet.Confirmed == false)
            {
                return;
            }

            var character = clientSession.Character;
            var pocket = packet.PocketType.ToNoscorePocketType();
            character.InventoryService.SortPocket(pocket);
            await clientSession.SendPacketAsync(character.InventoryService.GeneratePocket(pocket));
            await clientSession.SendPacketsAsync(character.GenerateQuicklist());
        }
    }
}
