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
using NosCore.GameObject.Services.UpgradeService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    // A shell dropped on a weapon or armour copies its options onto it and binds it.
    [UsedImplicitly]
    public sealed class ShellHandler(IShellOptionService shellOptionService, IRandomNumberSource random)
    {
        private const byte WeaponShell = 0;
        private const double ReplaceChance = 0.5;

        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var packet = evt.Packet;
            if (evt.InventoryItem.ItemInstance is not WearableInstance { Item.ItemType: ItemType.Shell } shell
                || packet.TargetType is not { } targetType || packet.TargetSlot is not { } targetSlot)
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;
            if (character.InventoryService.LoadBySlotAndType(targetSlot, targetType.ToNoscorePocketType())?.ItemInstance
                is not WearableInstance equipment || equipment.Id == shell.Id)
            {
                return;
            }

            var shellOptions = shellOptionService.Load(shell.Id);
            var message = shellOptions.Count == 0 ? Game18NConstString.CantInsertUnidentifiedShells
                : equipment.Item.ItemType != (shell.Item.ItemSubType == WeaponShell ? ItemType.Weapon : ItemType.Armor)
                    ? Game18NConstString.IncorrectEquipmentAndOptionType
                    : equipment.BoundCharacterId is { } owner && owner != character.CharacterId
                        ? Game18NConstString.ItemBoundToAnotherPlayer
                        : equipment.Rare < shell.Rare ? Game18NConstString.ItemRarityTooLow
                            : equipment.Item.LevelMinimum < shell.Upgrade ? Game18NConstString.NoItemInsertionLevelTooLow
                                : (Game18NConstString?)null;
            if (message is { } rejection)
            {
                await SendAsync(evt, rejection);
                return;
            }

            var replacing = shellOptionService.Load(equipment.Id).Count > 0;
            if (packet.Mode == 0)
            {
                await session.SendPacketAsync(new QnaiPacket
                {
                    YesPacket = new UseItemPacket
                    {
                        VisualType = packet.VisualType,
                        VisualId = character.CharacterId,
                        Type = packet.Type,
                        Slot = packet.Slot,
                        Mode = 1,
                        Parameter = packet.Parameter,
                        TargetType = targetType,
                        TargetSlot = targetSlot
                    },
                    Question = replacing ? Game18NConstString.ExistingOptionsWillBeRemoved : Game18NConstString.AskAddOptions
                });
                return;
            }

            var inventoryItem = evt.InventoryItem;
            character.InventoryService.RemoveItemAmountFromInventory(1, inventoryItem.ItemInstanceId);
            await session.SendPacketAsync((inventoryItem.ItemInstance.Amount > 0 ? inventoryItem : null).GeneratePocketChange(inventoryItem.Type.ToPocketType(), inventoryItem.Slot));
            if (replacing && random.NextDouble() >= ReplaceChance)
            {
                await shellOptionService.ClearAsync(shell.Id);
                await SendAsync(evt, Game18NConstString.ShellBroken);
                return;
            }

            await shellOptionService.ClearAsync(equipment.Id);
            await shellOptionService.SaveAsync(equipment.Id, shellOptions);
            await shellOptionService.ClearAsync(shell.Id);
            equipment.BoundCharacterId = character.CharacterId;
            equipment.ShellRarity = (byte)shell.Rare;
            await SendAsync(evt, replacing ? Game18NConstString.OptionChanged : Game18NConstString.OptionHasBeenAdded);
        }

        private static Task SendAsync(ItemUsedEvent evt, Game18NConstString message) =>
            evt.ClientSession.SendPacketAsync(new MsgiPacket { Type = MessageType.Default, Message = message });
    }
}
