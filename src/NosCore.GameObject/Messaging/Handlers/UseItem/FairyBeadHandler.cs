//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.FairyBeadService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    // An empty fairy bead opens the window the fairy is dropped into; a full one gives the fairy back.
    [UsedImplicitly]
    public sealed class FairyBeadHandler(IFairyBeadService fairyBeadService)
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var source = evt.InventoryItem;
            if (!fairyBeadService.IsFairyBead(source.ItemInstance) || source.ItemInstance is not BoxInstance bead)
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;
            if ((bead.HoldingVNum ?? 0) == 0)
            {
                await session.SendPacketAsync(new GuriPacket
                {
                    Type = GuriPacketType.AddFairyInPearl,
                    Argument = 0,
                    EntityId = source.Slot
                });
                return;
            }

            var inventory = character.InventoryService;
            var fairy = fairyBeadService.TakeOut(bead);
            if (!inventory.HasRoomFor(fairy))
            {
                await SendNotEnoughSpaceAsync(evt);
                return;
            }

            if (!inventory.TryConsume(source, 1))
            {
                return;
            }

            var added = inventory.AddItemToPocket(InventoryItemInstance.Create(fairy, character.CharacterId));
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
            await session.SendPacketAsync(character.GenerateSayi(SayColorType.Green, Game18NConstString.ReceivedThisItem,
                2, fairy.ItemVNum.ToString(), 1));
        }

        private static Task SendNotEnoughSpaceAsync(ItemUsedEvent evt) =>
            evt.ClientSession.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.NotEnoughSpace
            });
    }
}
