//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ItemGenerationService.Handlers
{
    public class SpRechargerEventHandler(IOptions<WorldConfiguration> worldConfiguration) : IUseItemEventHandler
    {
        public bool Condition(Item.Item item)
        {
            return (item.ItemType == ItemType.Special) &&
                (item.Effect >= ItemEffectType.DroppedSpRecharger) &&
                (item.Effect <= ItemEffectType.CraftedSpRecharger);
        }

        public async Task ExecuteAsync(RequestData<Tuple<InventoryItemInstance, UseItemPacket>> requestData)
        {
            var session = requestData.ClientSession;
            var character = session.Character;

            if (character.SpAdditionPoint < worldConfiguration.Value.MaxAdditionalSpPoints)
            {
                var itemInstance = requestData.Data.Item1;
                character.InventoryService.RemoveItemAmountFromInventory(1, itemInstance.ItemInstanceId);
                var pocketChangePacket = itemInstance.GeneratePocketChange((PocketType)itemInstance.Type, itemInstance.Slot);
                await session.SendPacketAsync(pocketChangePacket);

                character = session.Character;
                character.AddAdditionalSpPoints(itemInstance.ItemInstance.Item.EffectValue, worldConfiguration);
                var spPointPacket = character.GenerateSpPoint(worldConfiguration);
                await session.SendPacketAsync(spPointPacket);
            }
            else
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.CannotBeUsedExceedsCapacity
                });
            }
        }
    }
}
