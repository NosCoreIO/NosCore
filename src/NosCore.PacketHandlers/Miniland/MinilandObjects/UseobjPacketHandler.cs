//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Miniland;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.WarehouseHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.GameObject.Services.WarehouseService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Miniland;
using NosCore.Packets.ServerPackets.Warehouse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Miniland.MinilandObjects
{
    public class UseobjPacketHandler(IMinilandService minilandProvider, IWarehouseHub warehouseHttpClient, IDao<IItemInstanceDto?, Guid> itemInstanceDao, IItemGenerationService itemProvider)
        : PacketHandler<UseObjPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(UseObjPacket useobjPacket, ClientSession clientSession)
        {
            var miniland = minilandProvider.GetMiniland(clientSession.Character.CharacterId);
            var minilandObject =
                clientSession.Character.MapInstance.MapDesignObjects.Values.FirstOrDefault(s =>
                    s.Slot == useobjPacket.ObjectId);
            if (minilandObject == null)
            {
                return;
            }

            if (!minilandObject.InventoryItemInstance!.ItemInstance.Item.IsWarehouse)
            {
                var game = (byte)(minilandObject.InventoryItemInstance.ItemInstance.Item.EquipmentSlot ==
                    EquipmentType.MainWeapon
                        ? (4 + minilandObject.InventoryItemInstance.ItemInstance.ItemVNum) % 10
                        : (int)minilandObject.InventoryItemInstance.ItemInstance.Item.EquipmentSlot / 3);
                var full = false;
                await clientSession.SendPacketAsync(new MloInfoPacket
                {
                    IsOwner = miniland.MapInstanceId == clientSession.Character.MapInstanceId,
                    ObjectVNum = minilandObject.InventoryItemInstance.ItemInstance.ItemVNum,
                    Slot = (byte)useobjPacket.ObjectId,
                    MinilandPoints = miniland.MinilandPoint,
                    LawDurability = minilandObject.DurabilityPoint < 1000,
                    IsFull = full,
                    MinigamePoints = new MloInfoPacketSubPacket[]
                    {
                        new() {MinimumPoints = 0, MaximumPoints = MinilandHelper.Instance.MinilandMaxPoint[game][0]},
                        new()
                        {
                            MinimumPoints = MinilandHelper.Instance.MinilandMaxPoint[game][0] + 1,
                            MaximumPoints = MinilandHelper.Instance.MinilandMaxPoint[game][1]
                        },
                        new()
                        {
                            MinimumPoints = MinilandHelper.Instance.MinilandMaxPoint[game][1] + 1,
                            MaximumPoints = MinilandHelper.Instance.MinilandMaxPoint[game][2]
                        },
                        new()
                        {
                            MinimumPoints = MinilandHelper.Instance.MinilandMaxPoint[game][2] + 1,
                            MaximumPoints = MinilandHelper.Instance.MinilandMaxPoint[game][3]
                        },
                        new()
                        {
                            MinimumPoints = MinilandHelper.Instance.MinilandMaxPoint[game][3] + 1,
                            MaximumPoints = MinilandHelper.Instance.MinilandMaxPoint[game][4]
                        },
                        new()
                        {
                            MinimumPoints = MinilandHelper.Instance.MinilandMaxPoint[game][4] + 1,
                            MaximumPoints = MinilandHelper.Instance.MinilandMaxPoint[game][5]
                        }
                    }
                });
            }
            else
            {
                var links = await warehouseHttpClient.GetWarehouseItems(null, clientSession.Character.CharacterId,
                    WarehouseType.Warehouse, null);
                var warehouseItems = new List<WarehouseItem>();
                foreach (var warehouselink in links)
                {
                    var warehouseItem = warehouselink.Warehouse!.Adapt<WarehouseItem>();
                    var itemInstance = await itemInstanceDao.FirstOrDefaultAsync(s => s!.Id == warehouselink.ItemInstance!.Id);
                    warehouseItem.ItemInstance = itemProvider.Convert(itemInstance!);
                    warehouseItems.Add(warehouseItem);
                }
                await clientSession.SendPacketAsync(new StashAllPacket
                {
                    WarehouseSize =
                        (byte)minilandObject.InventoryItemInstance.ItemInstance.Item.MinilandObjectPoint,
                    IvnSubPackets = warehouseItems.Select(invItem =>
                        invItem.ItemInstance.GenerateIvnSubPacket((PocketType)invItem.ItemInstance!.Item.Type,
                            invItem.Slot)).ToList()
                });
            }
        }
    }
}
