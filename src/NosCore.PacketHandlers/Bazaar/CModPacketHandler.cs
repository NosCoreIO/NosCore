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

using Json.More;
using Json.Patch;
using Json.Pointer;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.HttpClients.BazaarHttpClient;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using Serilog;
using System.Threading.Tasks;

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

        public override async Task ExecuteAsync(CModPacket packet, ClientSession clientSession)
        {
            var bz = await _bazaarHttpClient.GetBazaarLinkAsync(packet.BazaarId).ConfigureAwait(false);
            if ((bz != null) && (bz.SellerName == clientSession.Character.Name) &&
                (bz.BazaarItem?.Price != packet.NewPrice))
            {
                if (bz.BazaarItem?.Amount != bz.ItemInstance?.Amount)
                {
                    await clientSession.SendPacketAsync(new ModaliPacket
                    {
                        Type = 1,
                        Message = Game18NConstString.CannotChangePriceSoldItems
                    }).ConfigureAwait(false);
                    return;
                }

                if (bz.BazaarItem?.Amount == packet.Amount)
                {
                    var patch = new JsonPatch(PatchOperation.Replace(JsonPointer.Create<BazaarLink>(o => o.BazaarItem!.Price), packet.NewPrice.AsJsonElement()));
                    var bzMod = await _bazaarHttpClient.ModifyAsync(packet.BazaarId, patch).ConfigureAwait(false);

                    if ((bzMod != null) && (bzMod.BazaarItem?.Price != bz.BazaarItem.Price))
                    {
                        await clientSession.HandlePacketsAsync(new[] {new CSListPacket {Index = 0, Filter = BazaarStatusType.Default}}).ConfigureAwait(false);
                        await clientSession.SendPacketAsync(new SayiPacket
                        {
                            VisualType = VisualType.Player,
                            VisualId = clientSession.Character.CharacterId,
                            Type = SayColorType.Yellow,
                            Message = Game18NConstString.NewSellingPrice,
                            FirstArgument = 4,
                            SecondArgument = (int)bz.BazaarItem.Price
                        }).ConfigureAwait(false);
                        return;
                    }
                }

                await clientSession.SendPacketAsync(new ModaliPacket
                {
                    Type = 1,
                    Message = Game18NConstString.OfferUpdated
                }).ConfigureAwait(false);

            }
            else
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.BAZAAR_MOD_ERROR));
            }
        }
    }
}