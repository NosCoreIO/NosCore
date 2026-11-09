//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.MateService
{
    public sealed class PartnerEquipmentService(IPartnerSpecialistService partnerSpecialistService)
        : IPartnerEquipmentService, ISingletonService
    {
        private const byte PartnerWeaponSubType = 12;
        private const byte PartnerArmorSubType = 4;
        private const byte PartnerSpecialistSubType = 4;

        public async Task WearAsync(PlayerComponentBundle character, short equipmentSlot, byte partnerSlot)
        {
            var mate = FindPartner(character, partnerSlot);
            var pocket = mate?.EquipmentPocket();
            var inventoryItem = character.InventoryService.LoadBySlotAndType(equipmentSlot, NoscorePocketType.Equipment);
            if (character.InExchangeOrShop || mate == null || pocket == null || inventoryItem?.ItemInstance?.Item == null)
            {
                return;
            }

            var item = inventoryItem.ItemInstance.Item;
            if (!CanWear(item.EquipmentSlot, item.ItemType, item.ItemSubType) || item.LevelMinimum > mate.Level)
            {
                await character.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = character.CharacterId,
                    Type = SayColorType.Yellow,
                    Message = Game18NConstString.PartnerCanNotUseThatEquipment
                });
                return;
            }

            character.InventoryService.MoveInPocket(equipmentSlot, NoscorePocketType.Equipment, pocket.Value,
                (short)item.EquipmentSlot, true);
            if (item.EquipmentSlot == EquipmentType.Sp)
            {
                await partnerSpecialistService.UntransformAsync(character, mate);
            }

            var swapped = character.InventoryService.LoadBySlotAndType(equipmentSlot, NoscorePocketType.Equipment);
            await character.SendPacketAsync(swapped.GeneratePocketChange(PocketType.Equipment, equipmentSlot));
            await character.SendPacketAsync(mate.GenerateScn(character.AccountLanguage, character.InventoryService));
        }

        public async Task RemoveAsync(PlayerComponentBundle character, EquipmentType slot, byte partnerSlot)
        {
            var mate = FindPartner(character, partnerSlot);
            var pocket = mate?.EquipmentPocket();
            if (character.InExchangeOrShop || mate == null || pocket == null
                || character.InventoryService.LoadBySlotAndType((short)slot, pocket.Value) == null)
            {
                return;
            }

            var removed = character.InventoryService.MoveInPocket((short)slot, pocket.Value, NoscorePocketType.Equipment);
            if (removed == null)
            {
                await character.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.NotEnoughSpace
                });
                return;
            }

            if (slot == EquipmentType.Sp)
            {
                await partnerSpecialistService.UntransformAsync(character, mate);
            }

            await character.SendPacketAsync(removed.GeneratePocketChange(PocketType.Equipment, removed.Slot));
            await character.SendPacketAsync(mate.GenerateScn(character.AccountLanguage, character.InventoryService));
        }

        private static Mate? FindPartner(PlayerComponentBundle character, byte partnerSlot) =>
            partnerSlot == 0
                ? null
                : character.Mates.Values.FirstOrDefault(s => s.MateType == MateType.Partner && s.PetSlot == partnerSlot - 1);

        private static bool CanWear(EquipmentType slot, ItemType itemType, byte subType) => slot switch
        {
            EquipmentType.Sp => itemType == ItemType.Specialist && subType == PartnerSpecialistSubType,
            EquipmentType.MainWeapon => subType == PartnerWeaponSubType,
            EquipmentType.Armor => subType == PartnerArmorSubType,
            EquipmentType.Gloves or EquipmentType.Boots => true,
            _ => false
        };
    }
}
