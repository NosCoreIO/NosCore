//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Miniland;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.WarehouseHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Miniland;
using NosCore.Packets.ServerPackets.Warehouse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UseObjPacket = NosCore.Packets.ClientPackets.Miniland.UseObjPacket;

namespace NosCore.PacketHandlers.Miniland.MinilandObjects
{
    public class UseobjPacketHandler(IMinilandService minilandProvider, IWarehouseHub warehouseHttpClient, IDao<IItemInstanceDto?, Guid> itemInstanceDao, IItemGenerationService itemProvider,
        IMinigameRewardService minigameRewards)
        : PacketHandler<UseObjPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(UseObjPacket useobjPacket, ClientSession clientSession)
        {
            var miniland = minilandProvider.GetMiniland(clientSession.Character.CharacterId);
            var minilandObject =
                clientSession.Character.MapInstance.MapDesignObjects.Values.FirstOrDefault(s =>
                    s.Slot == useobjPacket.Slot);
            if (minilandObject?.InventoryItemInstance?.ItemInstance is not { } instance)
            {
                return;
            }

            if (!instance.Item.IsWarehouse)
            {
                var maximums = minigameRewards.GetMaximumScores(instance.Item.MinigameType ?? MinigameType.Quarry);
                var full = false;
                await clientSession.SendPacketAsync(new MloInfoPacket
                {
                    IsOwner = miniland.MapInstanceId == clientSession.Character.MapInstanceId,
                    ObjectVNum = instance.ItemVNum,
                    Slot = (byte)useobjPacket.Slot,
                    MinilandPoints = miniland.MinilandPoint,
                    LawDurability = minilandObject.DurabilityPoint < 1000,
                    IsFull = full,
                    MinigamePoints = maximums.Select((max, level) => new MloInfoPacketSubPacket
                    {
                        MinimumPoints = level == 0 ? 0 : maximums[level - 1] + 1,
                        MaximumPoints = max
                    }).ToArray()
                });
            }
            else
            {
                var links = await warehouseHttpClient.GetWarehouseItems(null, clientSession.Character.CharacterId,
                    WarehouseType.Warehouse, null);
                var warehouseItems = new List<(short Slot, IItemInstance Item)>();
                foreach (var warehouselink in links)
                {
                    var itemInstance = await itemInstanceDao.FirstOrDefaultAsync(s => s.Id == warehouselink.ItemInstance.Id);
                    if (itemInstance != null)
                    {
                        warehouseItems.Add((warehouselink.Slot, itemProvider.Convert(itemInstance)));
                    }
                }
                await clientSession.SendPacketAsync(new StashAllPacket
                {
                    WarehouseSize =
                        (byte)instance.Item.MinilandObjectPoint,
                    Items = warehouseItems.Select(s => s.Item.GenerateWarehouseSubPacket(s.Slot)).ToList()
                });
            }
        }
    }
}
