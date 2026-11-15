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
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.FamilyService;
using NosCore.GameObject.Services.WarehouseService;
using NosCore.Data.Enumerations.Miniland;
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
            IItemGenerationService itemProvider, ISessionRegistry sessionRegistry,
            IPersonalWarehouseService personalWarehouse, IFamilyWarehouseService familyWarehouse)
        : PacketHandler<EquipmentInfoPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(EquipmentInfoPacket packet, ClientSession clientSession)
        {
            if (await FindAsync(packet, clientSession) is { } item && itemInfoService.GenerateInfo(item) is { } info)
            {
                await clientSession.SendPacketAsync(info);
            }
        }

        private async Task<IItemInstance?> FindAsync(EquipmentInfoPacket packet, ClientSession clientSession)
        {
            var character = clientSession.Character;
            return packet.Type switch
            {
                EquipmentInfoType.Warehouse => await personalWarehouse.GetItemAsync(clientSession, WarehouseType.Warehouse, packet.Slot),
                EquipmentInfoType.PartnerWarehouse => await personalWarehouse.GetItemAsync(clientSession, WarehouseType.PetWarehouse, packet.Slot),
                EquipmentInfoType.FamilyWarehouse => await familyWarehouse.GetItemAsync(clientSession, packet.Slot),
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
                // The slot is the cell of the partner's offer, in the order it was offered.
                EquipmentInfoType.Exchange => character.Exchange is { } exchange && exchange.PartnerId == packet.ShopOwnerId
                    ? exchange.Partner.ExchangeItems.Keys.ElementAtOrDefault(packet.Slot)?.ItemInstance
                    : null,
                EquipmentInfoType.PersonalShop => sessionRegistry.TryGetCharacter(
                        s => s.VisualId == packet.ShopOwnerId && s.MapInstanceId == character.MapInstanceId, out var owner)
                    ? owner.Shop?.ShopItems.Values.FirstOrDefault(i => i.Slot == packet.Slot)?.ItemInstance
                    : null,
                _ => null
            };

            IItemInstance? InPocket(NoscorePocketType pocket, short slot) =>
                character.InventoryService.LoadBySlotAndType(slot, pocket)?.ItemInstance;
        }
    }
}
