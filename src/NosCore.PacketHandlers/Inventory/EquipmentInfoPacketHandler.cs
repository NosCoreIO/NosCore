//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemInfoService;
using NosCore.Packets.ClientPackets.Player;

namespace NosCore.PacketHandlers.Inventory
{
    public class EquipmentInfoPacketHandler(IItemInfoService itemInfoService)
        : PacketHandler<EquipmentInfoPacket>, IWorldPacketHandler
    {
        private const byte Worn = 0;
        private const byte EquipmentPocket = 1;
        private const byte SpecialistPocket = 7;

        public override async Task ExecuteAsync(EquipmentInfoPacket packet, ClientSession clientSession)
        {
            NoscorePocketType? pocket = packet.Type switch
            {
                Worn => NoscorePocketType.Wear,
                EquipmentPocket => NoscorePocketType.Equipment,
                SpecialistPocket => NoscorePocketType.Specialist,
                _ => null
            };
            if (pocket == null)
            {
                return;
            }

            var item = clientSession.Character.InventoryService.LoadBySlotAndType(packet.Slot, pocket.Value);
            if (item == null || itemInfoService.GenerateInfo(item.ItemInstance) is not { } info)
            {
                return;
            }

            await clientSession.SendPacketAsync(info);
        }
    }
}
