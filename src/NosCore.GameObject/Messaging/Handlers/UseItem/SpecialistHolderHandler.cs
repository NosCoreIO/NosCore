//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.SpecialistHolderService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class SpecialistHolderHandler(ISpecialistHolderService specialistHolderService)
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var source = evt.InventoryItem;
            if (!specialistHolderService.IsSpecialistHolder(source.ItemInstance))
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;
            var holder = (BoxInstance)source.ItemInstance;
            if ((holder.HoldingVNum ?? 0) == 0)
            {
                await session.SendPacketAsync(new WopenPacket
                {
                    Type = WindowType.GoldenSPCardHolder,
                    Unknown = (byte)source.Slot
                });
                return;
            }

            var inventory = character.InventoryService;
            var card = specialistHolderService.TakeOut(holder);
            if (!inventory.HasRoomFor(card))
            {
                await SendNotEnoughSpaceAsync(evt);
                return;
            }

            if (!inventory.TryConsume(source, 1))
            {
                return;
            }

            var added = inventory.AddItemToPocket(InventoryItemInstance.Create(card, character.CharacterId));
            if (added == null || added.Count == 0)
            {
                inventory.RestoreConsumed(source, 1);
                await SendNotEnoughSpaceAsync(evt);
                return;
            }

            foreach (var inv in added)
            {
                await session.SendPacketAsync(inv.GeneratePocketChange(inv.Type.ToPocketType(), inv.Slot));
            }

            await session.SendPacketAsync(inventory.LoadBySlotAndType(source.Slot, source.Type)
                .GeneratePocketChange(source.Type.ToPocketType(), source.Slot));
            await session.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.SpecialistOutOfHolder
            });
        }

        private static Task SendNotEnoughSpaceAsync(ItemUsedEvent evt) =>
            evt.ClientSession.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.NotEnoughSpace
            });
    }
}
