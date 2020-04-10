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

using System;
using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Packets.Enumerations;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using Serilog;

namespace NosCore.PacketHandlers.Shops
{
    public class ShoppingPacketHandler : PacketHandler<ShoppingPacket>, IWorldPacketHandler
    {
        private readonly ILogger _logger;

        public ShoppingPacketHandler(ILogger logger)
        {
            _logger = logger;
        }

        public override async Task ExecuteAsync(ShoppingPacket shoppingPacket, ClientSession clientSession)
        {
            var shopRate = new Tuple<double, byte>(0, 0);
            IAliveEntity? aliveEntity;
            switch (shoppingPacket.VisualType)
            {
                case VisualType.Player:
                    aliveEntity = Broadcaster.Instance.GetCharacter(s => s.VisualId == shoppingPacket.VisualId);
                    break;
                case VisualType.Npc:
                    shopRate = clientSession.Character.GenerateShopRates();
                    aliveEntity =
                        clientSession.Character.MapInstance.Npcs.Find(s => s.VisualId == shoppingPacket.VisualId);
                    break;

                default:
                    _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.VISUALTYPE_UNKNOWN),
                        shoppingPacket.VisualType);
                    return;
            }

            if (aliveEntity == null)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.VISUALENTITY_DOES_NOT_EXIST));
                return;
            }


            await clientSession.SendPacketAsync(aliveEntity.GenerateNInv(shopRate.Item1, shoppingPacket.ShopType, shopRate.Item2)).ConfigureAwait(false);
        }
    }
}