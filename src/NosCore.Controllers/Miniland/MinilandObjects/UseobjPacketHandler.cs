//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Linq;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Miniland;
using NosCore.Packets.ServerPackets.Warehouse;
using NosCore.Data.Enumerations.Miniland;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Helper;
using NosCore.GameObject.HttpClients.WarehouseHttpClient;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.MinilandProvider;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Miniland.MinilandObjects
{
    public class UseobjPacketHandler : PacketHandler<UseObjPacket>, IWorldPacketHandler
    {
        private readonly IMinilandProvider _minilandProvider;
        private readonly IWarehouseHttpClient _warehouseHttpClient;

        public UseobjPacketHandler(IMinilandProvider minilandProvider, IWarehouseHttpClient warehouseHttpClient)
        {
            _minilandProvider = minilandProvider;
            _warehouseHttpClient = warehouseHttpClient;
        }

        public override Task Execute(UseObjPacket useobjPacket, ClientSession clientSession)
        {
            var miniland = _minilandProvider.GetMiniland(clientSession.Character.CharacterId);
            var minilandObject =
                clientSession.Character.MapInstance.MapDesignObjects.Values.FirstOrDefault(s =>
                    s.Slot == useobjPacket.ObjectId);
            if ((minilandObject != null) && (miniland != null))
            {
                if (!minilandObject.InventoryItemInstance.ItemInstance.Item.IsWarehouse)
                {
                    var game = (byte) (minilandObject.InventoryItemInstance.ItemInstance.Item.EquipmentSlot ==
                        EquipmentType.MainWeapon
                            ? (4 + minilandObject.InventoryItemInstance.ItemInstance.ItemVNum) % 10
                            : (int) minilandObject.InventoryItemInstance.ItemInstance.Item.EquipmentSlot / 3);
                    var full = false;
                    clientSession.SendPacket(new MloInfoPacket
                    {
                        IsOwner = miniland.MapInstanceId == clientSession.Character.MapInstanceId,
                        ObjectVNum = minilandObject.InventoryItemInstance.ItemInstance.ItemVNum,
                        Slot = (byte) useobjPacket.ObjectId,
                        MinilandPoints = miniland.MinilandPoint,
                        LawDurability = minilandObject.DurabilityPoint < 1000,
                        IsFull = full,
                        MinigamePoints = new MloInfoPacketSubPacket[6]
                        {
                            new MloInfoPacketSubPacket
                                {MinimumPoints = 0, MaximumPoints = MinilandHelper.Instance.MinilandMaxPoint[game][0]},
                            new MloInfoPacketSubPacket
                            {
                                MinimumPoints = MinilandHelper.Instance.MinilandMaxPoint[game][0] + 1,
                                MaximumPoints = MinilandHelper.Instance.MinilandMaxPoint[game][1]
                            },
                            new MloInfoPacketSubPacket
                            {
                                MinimumPoints = MinilandHelper.Instance.MinilandMaxPoint[game][1] + 1,
                                MaximumPoints = MinilandHelper.Instance.MinilandMaxPoint[game][2]
                            },
                            new MloInfoPacketSubPacket
                            {
                                MinimumPoints = MinilandHelper.Instance.MinilandMaxPoint[game][2] + 1,
                                MaximumPoints = MinilandHelper.Instance.MinilandMaxPoint[game][3]
                            },
                            new MloInfoPacketSubPacket
                            {
                                MinimumPoints = MinilandHelper.Instance.MinilandMaxPoint[game][3] + 1,
                                MaximumPoints = MinilandHelper.Instance.MinilandMaxPoint[game][4]
                            },
                            new MloInfoPacketSubPacket
                            {
                                MinimumPoints = MinilandHelper.Instance.MinilandMaxPoint[game][4] + 1,
                                MaximumPoints = MinilandHelper.Instance.MinilandMaxPoint[game][5]
                            }
                        }
                    });
                }
                else
                {
                    var warehouseItems = _warehouseHttpClient.GetWarehouseItems(clientSession.Character.CharacterId,
                        WarehouseType.Warehouse);
                    clientSession.SendPacket(new StashAllPacket
                    {
                        WarehouseSize =
                            (byte) minilandObject.InventoryItemInstance.ItemInstance.Item.MinilandObjectPoint,
                        IvnSubPackets = warehouseItems.Select(invItem =>
                            invItem.ItemInstance.GenerateIvnSubPacket((PocketType) invItem.ItemInstance.Item.Type,
                                invItem.Slot)).ToList()
                    });
                }
            }
            return Task.CompletedTask;
        }
    }
}