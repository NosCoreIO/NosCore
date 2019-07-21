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

using ChickenAPI.Packets.ClientPackets.Bazaar;
using ChickenAPI.Packets.ClientPackets.Shops;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.Bazaar;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Configuration;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.HttpClients.BazaarHttpClient;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.PacketHandlers.CharacterScreen
{
    public class CModPacketHandler : PacketHandler<CModPacket>, IWorldPacketHandler
    {
        private readonly IBazaarHttpClient _bazaarHttpClient;
        private readonly ILogger _logger;

        public CModPacketHandler(IBazaarHttpClient bazaarHttpClient, ILogger logger)
        {
            _bazaarHttpClient = bazaarHttpClient;
            _logger = logger;
        }

        public override void Execute(CModPacket packet, ClientSession clientSession)
        {
            if (clientSession.Character.InExchangeOrTrade)
            {
                return;
            }

            var bz = _bazaarHttpClient.GetBazaarLinks(packet.BazaarId).FirstOrDefault();
            if (bz != null && bz.SellerName == clientSession.Character.Name)
            {
                //var remove = _bazaarHttpClient.Patch();
                //if (remove)
                //{
                //    clientSession.SendPacket(new RCScalcPacket
                //    {
                //        Type = VisualType.Player,
                //        Price = bz.BazaarItem.Price,
                //        RemainingAmount = (short)(bz.BazaarItem.Amount - bz.ItemInstance.Amount),
                //        Amount = bz.BazaarItem.Amount,
                //        Taxes = taxes,
                //        Total = price + taxes
                //    });
                //    clientSession.HandlePackets(new[] { new CSListPacket { Index = 0, Filter = BazaarStatusType.Default } });
                //}
                //else
                //{
                //    _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.BAZAAR_DELETE_ERROR));
                //}
            }
            else
            {
              
            }
        }
    }
}