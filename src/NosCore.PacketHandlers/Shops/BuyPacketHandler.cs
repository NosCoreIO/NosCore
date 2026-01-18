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

using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.ShopService;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Shops
{
    public class BuyPacketHandler(ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage,
            ISessionRegistry sessionRegistry, IShopRegistry shopRegistry)
        : PacketHandler<BuyPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(BuyPacket buyPacket, ClientSession clientSession)
        {
            Shop? shop = null;
            switch (buyPacket.VisualType)
            {
                case VisualType.Player:
                    var character = sessionRegistry.GetPlayer(s => s.VisualId == buyPacket.VisualId);
                    shop = character?.Shop;
                    break;
                case VisualType.Npc:
                    var npc = clientSession.Player.MapInstance.GetNpc((int)buyPacket.VisualId);
                    shop = npc != null ? shopRegistry.GetShop(npc.Value) : null;
                    break;

                default:
                    logger.Error(logLanguage[LogLanguageKey.VISUALTYPE_UNKNOWN],
                        buyPacket.VisualType);
                    return Task.CompletedTask;
            }

            if (shop != null)
            {
                // TODO: BuyAsync logic should be moved to a shop service
                // return clientSession.Character.BuyAsync(shop, buyPacket.Slot, buyPacket.Amount);
                return Task.CompletedTask;
            }

            logger.Error(logLanguage[LogLanguageKey.VISUALENTITY_DOES_NOT_EXIST]);
            return Task.CompletedTask;

        }
    }
}