//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.TransformationService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ItemGenerationService.Handlers
{
    public class VehicleEventHandler(ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage,
            ITransformationService transformationService)
        : IUseItemEventHandler
    {
        public bool Condition(Item.Item item)
        {
            return (item.ItemType == ItemType.Special) && (item.Effect == ItemEffectType.Vehicle);
        }

        public async Task ExecuteAsync(RequestData<Tuple<InventoryItemInstance, UseItemPacket>> requestData)
        {
            var itemInstance = requestData.Data.Item1;
            var packet = requestData.Data.Item2;
            if (requestData.ClientSession.Character.InExchangeOrShop)
            {
                logger.Error(logLanguage[LogLanguageKey.CANT_USE_ITEM_IN_SHOP]);
                return;
            }

            if ((packet.Mode == 1) && !requestData.ClientSession.Character.IsVehicled)
            {
                await requestData.ClientSession.SendPacketAsync(new DelayPacket
                {
                    Type = DelayPacketType.Locomotion,
                    Delay = 3000,
                    Packet = requestData.ClientSession.Character.GenerateUseItem((PocketType)itemInstance.Type,
                        itemInstance.Slot,
                        2, 0)
                });
                return;
            }

            if ((packet.Mode == 2) && !requestData.ClientSession.Character.IsVehicled)
            {
                await transformationService.ChangeVehicleAsync(requestData.ClientSession.Character, itemInstance.ItemInstance.Item);
                return;
            }

            await transformationService.RemoveVehicleAsync(requestData.ClientSession.Character);
        }
    }
}
