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
            if (character.InExchangeOrShop)
            {
                return;
            }

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

            var card = specialistHolderService.TakeOut(holder);
            var added = character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(card, character.CharacterId));
            if (added == null || added.Count == 0)
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.NotEnoughSpace
                });
                return;
            }

            foreach (var inv in added)
            {
                await session.SendPacketAsync(inv.GeneratePocketChange((PocketType)inv.Type, inv.Slot));
            }

            character.InventoryService.RemoveItemAmountFromInventory(1, source.ItemInstanceId);
            await session.SendPacketAsync(source.GeneratePocketChange((PocketType)source.Type, source.Slot));
            await session.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.SpecialistOutOfHolder
            });
        }
    }
}
