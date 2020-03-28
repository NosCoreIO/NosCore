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

using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using Microsoft.AspNetCore.JsonPatch;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.HttpClients.BazaarHttpClient;
using NosCore.GameObject.Networking.ClientSession;
using Serilog;

namespace NosCore.PacketHandlers.Bazaar
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

        public override async Task Execute(CModPacket packet, ClientSession clientSession)
        {
            if (clientSession.Character.InExchangeOrTrade)
            {
                return;
            }

            var bz = await _bazaarHttpClient.GetBazaarLink(packet.BazaarId);
            if ((bz != null) && (bz.SellerName == clientSession.Character.Name) &&
                (bz.BazaarItem.Price != packet.NewPrice))
            {
                if (bz.BazaarItem.Amount != bz.ItemInstance.Amount)
                {
                    clientSession.SendPacket(new ModalPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.CAN_NOT_MODIFY_SOLD_ITEMS,
                            clientSession.Account.Language),
                        Type = 1
                    });
                    return;
                }

                if (bz.BazaarItem.Amount == packet.Amount)
                {
                    var patch = new JsonPatchDocument<BazaarLink>();
                    patch.Replace(link => link.BazaarItem.Price, packet.NewPrice);
                    var bzMod = await _bazaarHttpClient.Modify(packet.BazaarId, patch);

                    if ((bzMod != null) && (bzMod.BazaarItem.Price != bz.BazaarItem.Price))
                    {
                        await clientSession.HandlePackets(new[]
                            {new CSListPacket {Index = 0, Filter = BazaarStatusType.Default}});
                        clientSession.SendPacket(clientSession.Character.GenerateSay(
                            string.Format(
                                Language.Instance.GetMessageFromKey(LanguageKey.BAZAAR_PRICE_CHANGED,
                                    clientSession.Account.Language),
                                bz.BazaarItem.Price
                            ), SayColorType.Yellow));
                        return;
                    }
                }

                clientSession.SendPacket(new ModalPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.STATE_CHANGED_BAZAAR,
                        clientSession.Account.Language),
                    Type = 1
                });
            }
            else
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.BAZAAR_MOD_ERROR));
            }
        }
    }
}