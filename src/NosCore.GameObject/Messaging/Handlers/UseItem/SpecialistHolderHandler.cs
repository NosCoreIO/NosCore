//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.SpecialistHolderService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class SpecialistHolderHandler(ISpecialistHolderService specialistHolderService)
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var source = evt.InventoryItem;
            var forPartner = specialistHolderService.IsPartnerSpecialistHolder(source.ItemInstance);
            if (!forPartner && !specialistHolderService.IsSpecialistHolder(source.ItemInstance))
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;
            var holder = (BoxInstance)source.ItemInstance;
            if ((holder.HoldingVNum ?? 0) == 0 && forPartner)
            {
                await StorePartnerCardAsync(evt, holder);
                return;
            }

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

        private async Task StorePartnerCardAsync(ItemUsedEvent evt, BoxInstance holder)
        {
            var source = evt.InventoryItem;
            var owner = evt.ClientSession.Character;
            // The client names the partner the player has selected as the target of the item.
            if (evt.Packet.VisualType != VisualType.Npc
                || !owner.Mates.TryGetValue(evt.Packet.VisualId, out var partner)
                || partner.MateType != MateType.Partner
                || partner.EquipmentPocket() is not { } pocket
                || partner.Specialist(owner.InventoryService) is not { } card)
            {
                await SendAsync(evt, Game18NConstString.NoSpecialistCardEquipped);
                return;
            }

            if (partner.SpecialistMorph != null)
            {
                await SendAsync(evt, Game18NConstString.CantPutSpecialistInHolderWhileTransformed);
                return;
            }

            if (!specialistHolderService.CanStore(card))
            {
                await SendAsync(evt, Game18NConstString.NotBeAbleToPutTheSelectedSpecialistCardIntoCardHolder);
                return;
            }

            if (evt.Packet.Mode == 0)
            {
                await owner.SendPacketAsync(new QnaiPacket
                {
                    YesPacket = new UseItemPacket
                    {
                        VisualType = VisualType.Npc,
                        VisualId = partner.MateTransportId,
                        Type = source.Type.ToPocketType(),
                        Slot = source.Slot,
                        Mode = 1,
                        Parameter = 0
                    },
                    Question = Game18NConstString.AskPutSpecialistInHolder
                });
                return;
            }

            if (owner.InventoryService.DeleteFromTypeAndSlot(pocket, (short)EquipmentType.Sp) != null)
            {
                return;
            }

            specialistHolderService.Store(card, holder);
            await owner.SendPacketAsync(source.GeneratePocketChange(source.Type.ToPocketType(), source.Slot));
            await owner.SendPacketAsync(partner.GenerateScn(owner.AccountLanguage, owner.InventoryService));
            await SendAsync(evt, Game18NConstString.SpecialistInTheHolder);
        }

        private static Task SendNotEnoughSpaceAsync(ItemUsedEvent evt) => SendAsync(evt, Game18NConstString.NotEnoughSpace);

        private static Task SendAsync(ItemUsedEvent evt, Game18NConstString message) =>
            evt.ClientSession.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = message
            });
    }
}
