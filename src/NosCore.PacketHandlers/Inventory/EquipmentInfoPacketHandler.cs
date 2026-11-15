//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.ItemInfoService;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Data.Enumerations.Character;

namespace NosCore.PacketHandlers.Inventory
{
    public class EquipmentInfoPacketHandler(IItemInfoService itemInfoService, IItemCatalog itemCatalog,
            IItemGenerationService itemProvider)
        : PacketHandler<EquipmentInfoPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(EquipmentInfoPacket packet, ClientSession clientSession)
        {
            if (Find(packet, clientSession) is { } item && itemInfoService.GenerateInfo(item) is { } info)
            {
                await clientSession.SendPacketAsync(info);
            }
        }

        private IItemInstance? Find(EquipmentInfoPacket packet, ClientSession clientSession)
        {
            var character = clientSession.Character;
            return packet.Type switch
            {
                EquipmentInfoType.Worn => InPocket(NoscorePocketType.Wear, packet.Slot),
                EquipmentInfoType.Equipment => InPocket(NoscorePocketType.Equipment, packet.Slot),
                EquipmentInfoType.Specialist => InPocket(NoscorePocketType.Specialist, packet.Slot),
                EquipmentInfoType.Costume => InPocket(NoscorePocketType.Costume, packet.Slot),
                // The slot is the partner's position counted from 1, the second field its equipment slot.
                EquipmentInfoType.PartnerEquipment => character.Mates.Values
                        .FirstOrDefault(m => m.MateType == MateType.Partner && m.PetSlot == packet.Slot - 1)?
                        .EquipmentPocket() is { } pocket
                    ? InPocket(pocket, packet.MateSlot)
                    : null,
                // An NPC shop sends the item's VNum as the slot.
                EquipmentInfoType.NpcShop => itemCatalog.GetItem(packet.Slot) == null ? null : itemProvider.Create(packet.Slot),
                _ => null
            };

            IItemInstance? InPocket(NoscorePocketType pocket, short slot) =>
                character.InventoryService.LoadBySlotAndType(slot, pocket)?.ItemInstance;
        }
    }
}
