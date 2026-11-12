//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class PlayEffectHandler
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var item = evt.InventoryItem.ItemInstance.Item;
            if (item.Effect != ItemEffectType.PlayEffect)
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;
            var itemInstance = evt.InventoryItem;
            character.InventoryService.RemoveItemAmountFromInventory(1, itemInstance.ItemInstanceId);
            await session.SendPacketAsync(itemInstance.GeneratePocketChange(itemInstance.Type.ToPocketType(), itemInstance.Slot));
            await character.MapInstance.SendPacketAsync(character.GenerateEff(item.EffectValue));
            if (item.UseSound > 0)
            {
                await character.MapInstance.SendPacketAsync(new GuriPacket
                {
                    Type = GuriPacketType.AfterSumming,
                    Argument = 1,
                    SecondArgument = 0,
                    EntityId = character.CharacterId,
                    Value = (uint)item.UseSound,
                });
            }
        }
    }
}
