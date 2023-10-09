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
using Json.More;
using Json.Patch;
using Json.Pointer;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System.Threading.Tasks;
using NosCore.GameObject.InterChannelCommunication.Hubs.BazaarHub;

namespace NosCore.PacketHandlers.Bazaar
{
    public class CModPacketHandler(IBazaarHub bazaarHttpClient, ILogger logger,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        : PacketHandler<CModPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(CModPacket packet, ClientSession clientSession)
        {
            var bzs = await bazaarHttpClient.GetBazaar(packet.BazaarId,null,null,null,null,null,null,null,null).ConfigureAwait(false);
            var bz = bzs.FirstOrDefault();
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
                    var patch = new JsonPatch(PatchOperation.Replace(JsonPointer.Create<BazaarLink>(o => o.BazaarItem!.Price), packet.NewPrice.AsJsonElement().AsNode()));
                    var bzMod = await bazaarHttpClient.ModifyBazaarAsync(packet.BazaarId, patch).ConfigureAwait(false);

                    if ((bzMod != null) && (bzMod.BazaarItem?.Price != bz.BazaarItem.Price))
                    {
                        await clientSession.HandlePacketsAsync(new[] {new CSListPacket {Index = 0, Filter = BazaarStatusType.Default}}).ConfigureAwait(false);
                        await clientSession.SendPacketAsync(new SayiPacket
                        {
                            VisualType = VisualType.Player,
                            VisualId = clientSession.Character.CharacterId,
                            Type = SayColorType.Yellow,
                            Message = Game18NConstString.NewSellingPrice,
                            ArgumentType = 4,
                            Game18NArguments = { bzMod.BazaarItem?.Price ?? 0 }
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
                logger.Error(logLanguage[LogLanguageKey.BAZAAR_MOD_ERROR]);
            }
        }
    }
}