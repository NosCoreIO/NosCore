//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Miniland;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Mates;
using NosCore.Packets.ServerPackets.Warehouse;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.WarehouseService
{
    // The miniland warehouse and the partner backpack: both belong to the character and differ only in size.
    public class PersonalWarehouseService(IWarehouseTransferService transfer) : IPersonalWarehouseService
    {
        private const int PartnerBackpackSize = 50;

        public async Task SendCarriedStoragesAsync(ClientSession session)
        {
            var bonuses = session.Character.StaticBonusList;
            if (bonuses.Any(s => s.StaticBonusType == StaticBonusType.PetBasket))
            {
                await session.SendPacketAsync(new IbPacket { Storage = CarriedStorageType.PetBasket, IsAvailable = true });
            }

            if (bonuses.Any(s => s.StaticBonusType == StaticBonusType.PetBackPack))
            {
                await session.SendPacketAsync(new IbPacket { Storage = CarriedStorageType.PartnerBackpack, IsAvailable = true });
            }
        }

        public async Task OpenAsync(ClientSession session, WarehouseType warehouseType)
        {
            var slots = Slots(session, warehouseType);
            var items = (await transfer.GetItemsAsync(slots.OwnerId, slots.Type))
                .Select(s => s.Item.GenerateWarehouseSubPacket(s.Slot)).ToList();
            IPacket packet = warehouseType == WarehouseType.PetWarehouse
                ? new PStashAllPacket { WarehouseSize = (byte)slots.Size, Items = items }
                : new StashAllPacket { WarehouseSize = (byte)slots.Size, Items = items };
            await session.SendPacketAsync(packet);
        }

        public Task DepositAsync(ClientSession session, WarehouseType warehouseType, PocketType pocket, short slot,
            short amount, short warehouseSlot) =>
            session.Character.InExchangeOrShop || pocket is PocketType.Miniland
                ? Task.CompletedTask
                : transfer.DepositAsync(session, Slots(session, warehouseType), pocket, slot, amount, warehouseSlot, _ => true);

        public Task WithdrawAsync(ClientSession session, WarehouseType warehouseType, short warehouseSlot, short amount) =>
            session.Character.InExchangeOrShop
                ? Task.CompletedTask
                : transfer.WithdrawAsync(session, Slots(session, warehouseType), warehouseSlot, amount);

        public Task MoveAsync(ClientSession session, WarehouseType warehouseType, short sourceSlot, short amount,
            short destinationSlot) =>
            session.Character.InExchangeOrShop
                ? Task.CompletedTask
                : transfer.MoveAsync(session, Slots(session, warehouseType), sourceSlot, amount, destinationSlot);

        private static WarehouseSlots Slots(ClientSession session, WarehouseType warehouseType)
        {
            var character = session.Character;
            var size = warehouseType == WarehouseType.PetWarehouse
                ? character.StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.PetBackPack) ? PartnerBackpackSize : 0
                : character.InventoryService.Values
                    .Where(s => s.Type == NoscorePocketType.Miniland && s.ItemInstance.Item.IsWarehouse)
                    .Select(s => (int)s.ItemInstance.Item.MinilandObjectPoint)
                    .DefaultIfEmpty(0)
                    .Max();
            return new WarehouseSlots(character.CharacterId, warehouseType, size);
        }
    }
}
