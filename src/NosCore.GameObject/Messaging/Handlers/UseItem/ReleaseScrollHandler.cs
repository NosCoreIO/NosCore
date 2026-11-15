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
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class ReleaseScrollHandler
    {
        private const int ReleaseEffect = 3003;

        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var scroll = evt.InventoryItem;
            if (scroll.ItemInstance.Item.Effect != ItemEffectType.ReleaseScroll
                || evt.Packet.TargetType is not { } targetType || evt.Packet.TargetSlot is not { } targetSlot)
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;
            var equipment = character.InventoryService.LoadBySlotAndType(targetSlot, (NoscorePocketType)targetType);
            if (equipment?.ItemInstance is not WearableInstance wearable)
            {
                return;
            }

            if (wearable.IsFixed != true)
            {
                await session.SendPacketAsync(character.GenerateSayi(SayColorType.Red, Game18NConstString.UseOnlyOnFixEquipment));
                return;
            }

            if (evt.Packet.Mode == 0)
            {
                await session.SendPacketAsync(new QnaiPacket
                {
                    YesPacket = new UseItemPacket
                    {
                        VisualType = VisualType.Player,
                        VisualId = character.CharacterId,
                        Type = scroll.Type.ToPocketType(),
                        Slot = scroll.Slot,
                        Mode = 1,
                        Parameter = 0,
                        TargetType = targetType,
                        TargetSlot = targetSlot
                    },
                    Question = Game18NConstString.AskWantToUse,
                    ArgumentType = 2,
                    Game18NArguments = { scroll.ItemInstance.ItemVNum.ToString() }
                });
                return;
            }

            wearable.IsFixed = false;
            await character.UseUpOneAsync(scroll);
            await session.SendPacketAsync(equipment.GeneratePocketChange(equipment.Type.ToPocketType(), equipment.Slot));
            await character.MapInstance.SendPacketAsync(character.GenerateEff(ReleaseEffect));
            await session.SendPacketAsync(character.GenerateSayi(SayColorType.Green, Game18NConstString.CanModifyEquipmentLevel));
        }
    }
}
