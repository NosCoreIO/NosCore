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

using Arch.Core;
using NosCore.Algorithm.DignityService;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Systems;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.ShopService;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Shops
{
    public class ShoppingPacketHandler(ILogger logger, IDignityService dignityService,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage, ISessionRegistry sessionRegistry, IEntityPacketSystem entityPacketSystem,
            IShopRegistry shopRegistry)
        : PacketHandler<ShoppingPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(ShoppingPacket shoppingPacket, ClientSession clientSession)
        {
            var percent = 0d;
            PlayerContext? player = null;
            Entity? npcEntity = null;
            Shop? shop = null;

            switch (shoppingPacket.VisualType)
            {
                case VisualType.Player:
                    player = sessionRegistry.GetPlayer(s => s.VisualId == shoppingPacket.VisualId);
                    shop = player?.Shop;
                    break;
                case VisualType.Npc:
                    percent = (dignityService.GetLevelFromDignity(clientSession.Player.Dignity)) switch
                    {
                        DignityType.Dreadful => 1.1,
                        DignityType.Unqualified => 1.2,
                        DignityType.Failed => 1.5,
                        DignityType.Useless => 1.5,
                        _ => 1.0,
                    };
                    npcEntity = clientSession.Player.MapInstance.GetNpc((int)shoppingPacket.VisualId);
                    shop = npcEntity != null ? shopRegistry.GetShop(npcEntity.Value) : null;
                    break;

                default:
                    logger.Error(logLanguage[LogLanguageKey.VISUALTYPE_UNKNOWN],
                        shoppingPacket.VisualType);
                    return;
            }

            if (player == null && npcEntity == null)
            {
                logger.Error(logLanguage[LogLanguageKey.VISUALENTITY_DOES_NOT_EXIST]);
                return;
            }

            if (shop?.ShopItems.IsEmpty == false)
            {
                if (player.HasValue)
                {
                    await clientSession.SendPacketAsync(entityPacketSystem.GenerateNInv(player.Value, shop, percent, shoppingPacket.ShopType))
                        .ConfigureAwait(false);
                }
                else if (npcEntity != null)
                {
                    var world = clientSession.Player.MapInstance.EcsWorld;
                    var visualId = npcEntity.Value.GetVisualId(world);
                    await clientSession.SendPacketAsync(GenerateNInvForNpc(visualId, shop, percent, shoppingPacket.ShopType))
                        .ConfigureAwait(false);
                }
            }
        }

        private static NosCore.Packets.ServerPackets.Shop.NInvPacket GenerateNInvForNpc(long visualId, Shop shop, double percent, short typeShop)
        {
            var shopItemList = new System.Collections.Generic.List<NosCore.Packets.ServerPackets.Shop.NInvItemSubPacket?>();
            var list = shop.ShopItems.Values.Where(s => s.Type == typeShop).ToList();
            for (var i = 0; i < shop.Size; i++)
            {
                var item = list.Find(s => s.Slot == i);
                if (item == null)
                {
                    shopItemList.Add(null);
                }
                else
                {
                    shopItemList.Add(new NosCore.Packets.ServerPackets.Shop.NInvItemSubPacket
                    {
                        Type = (NosCore.Packets.Enumerations.PocketType)item.ItemInstance!.Item.Type,
                        Slot = item.Slot,
                        Price = (int)(item.Price ?? (item.ItemInstance.Item.ReputPrice > 0
                            ? item.ItemInstance.Item.ReputPrice : item.ItemInstance.Item.Price * percent)),
                        RareAmount = item.ItemInstance.Item.Type == (byte)NosCore.Data.Enumerations.NoscorePocketType.Equipment
                            ? item.ItemInstance.Rare
                            : item.Amount,
                        UpgradeDesign = item.ItemInstance.Item.Type == (byte)NosCore.Data.Enumerations.NoscorePocketType.Equipment
                            ? item.ItemInstance.Item.IsColored
                                ? item.ItemInstance.Item.Color : item.ItemInstance.Upgrade : (short?)null,
                        VNum = item.ItemInstance.Item.VNum
                    });
                }
            }

            return new NosCore.Packets.ServerPackets.Shop.NInvPacket
            {
                VisualType = VisualType.Npc,
                VisualId = visualId,
                ShopKind = (byte)(percent * 100),
                Unknown = 0,
                Items = shopItemList
            };
        }
    }
}