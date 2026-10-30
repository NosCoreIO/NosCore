//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;

namespace NosCore.PacketHandlers.Inventory
{
    public class SortOpenPacketHandler : PacketHandler<SortOpenPacket>, IWorldPacketHandler
    {
        private static readonly NoscorePocketType[] SortedPockets =
            [NoscorePocketType.Specialist, NoscorePocketType.Costume];

        public override async Task ExecuteAsync(SortOpenPacket packet, ClientSession clientSession)
        {
            if (clientSession.Character.InExchangeOrShop)
            {
                return;
            }

            var inventory = clientSession.Character.InventoryService;
            foreach (var pocket in SortedPockets)
            {
                inventory.SortPocket(pocket, clientSession.Account.Language);
                await clientSession.SendPacketAsync(new InvPacket
                {
                    Type = (PocketType)pocket,
                    IvnSubPackets = inventory.Select(s => s.Value)
                        .Where(i => i.Type == pocket)
                        .OrderBy(i => i.Slot)
                        .Select(GenerateSubPacket)
                        .ToList()
                });
            }
        }

        private static IvnSubPacket GenerateSubPacket(InventoryItemInstance item) => new()
        {
            Slot = item.Slot,
            VNum = item.ItemInstance.ItemVNum,
            RareAmount = item.ItemInstance.Rare,
            UpgradeDesign = item.ItemInstance.Upgrade,
            SecondUpgrade = (item.ItemInstance as SpecialistInstance)?.SpStoneUpgrade ?? 0
        };
    }
}
