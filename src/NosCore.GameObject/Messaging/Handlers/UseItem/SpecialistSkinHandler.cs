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
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class SpecialistSkinHandler
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var skin = evt.InventoryItem.ItemInstance.Item;
            if (skin.ItemType != ItemType.Special || skin.Effect != ItemEffectType.SpecialistSkin)
            {
                return;
            }

            var character = evt.ClientSession.Character;
            if (character.InventoryService.LoadBySlotAndType(EquipmentType.Sp, NoscorePocketType.Wear)?.ItemInstance
                is not SpecialistInstance card)
            {
                await SendAsync(evt, Game18NConstString.NoSpecialistCardEquipped);
                return;
            }

            if (card.Item.Morph != skin.EffectValue)
            {
                await SendAsync(evt, Game18NConstString.SkinNotCompatibleCard);
                return;
            }

            if (card.Skin == skin.Morph)
            {
                await SendAsync(evt, Game18NConstString.AlreadyWearingSameMask);
                return;
            }

            card.Skin = skin.Morph;
            await character.UseUpOneAsync(evt.InventoryItem);
            if (!character.UseSp || character.IsVehicled)
            {
                return;
            }

            character.Morph = card.ShownMorph;
            await character.MapInstance.SendPacketAsync(character.GenerateCMode());
        }

        private static Task SendAsync(ItemUsedEvent evt, Game18NConstString message) =>
            evt.ClientSession.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = message
            });
    }
}
