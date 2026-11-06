//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.ShellOptionService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    // Dropped on a weapon or armour, removes every shell option from it.
    [UsedImplicitly]
    public sealed class MagicEraserHandler(IShellOptionService shellOptionService)
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var packet = evt.Packet;
            if (evt.InventoryItem.ItemInstance.Item.Effect != ItemEffectType.ShellOptionRemoval
                || packet.TargetType is not { } targetType || packet.TargetSlot is not { } targetSlot)
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;
            if (character.InventoryService.LoadBySlotAndType(targetSlot, targetType.ToNoscorePocketType())?.ItemInstance
                is not WearableInstance equipment)
            {
                return;
            }

            if (equipment.Item.ItemType is not (ItemType.Weapon or ItemType.Armor))
            {
                await SendAsync(evt, Game18NConstString.YouCanOnlyRemoveArmorAndWeaponOption);
                return;
            }

            if (shellOptionService.Load(equipment.Id).Count == 0)
            {
                await SendAsync(evt, Game18NConstString.NoOptionsOnItem);
                return;
            }

            await shellOptionService.ClearAsync(equipment.Id);
            equipment.ShellRarity = null;
            var eraser = evt.InventoryItem;
            character.InventoryService.RemoveItemAmountFromInventory(1, eraser.ItemInstanceId);
            await session.SendPacketAsync((eraser.ItemInstance.Amount > 0 ? eraser : null).GeneratePocketChange((PocketType)eraser.Type, eraser.Slot));
            await SendAsync(evt, Game18NConstString.AllOptionsRemoved);
        }

        private static Task SendAsync(ItemUsedEvent evt, Game18NConstString message) =>
            evt.ClientSession.SendPacketAsync(new MsgiPacket { Type = MessageType.Default, Message = message });
    }
}
