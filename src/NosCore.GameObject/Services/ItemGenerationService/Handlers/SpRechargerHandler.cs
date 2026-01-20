//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.ComponentEntities.Extensions;
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
            if (requestData.ClientSession.Character.SpAdditionPoint < worldConfiguration.Value.MaxAdditionalSpPoints)
            {
                var itemInstance = requestData.Data.Item1;
                requestData.ClientSession.Character.InventoryService.RemoveItemAmountFromInventory(1,
                    itemInstance.ItemInstanceId);
                await requestData.ClientSession.SendPacketAsync(
                    itemInstance.GeneratePocketChange((PocketType)itemInstance.Type, itemInstance.Slot));
                await requestData.ClientSession.Character.AddAdditionalSpPointsAsync(itemInstance.ItemInstance.Item.EffectValue, worldConfiguration);
            }
            else
            {
                await requestData.ClientSession.Character.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.CannotBeUsedExceedsCapacity
                });
            }
        }
    }
}
