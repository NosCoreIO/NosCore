//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Json.More;
using Json.Patch;
using Json.Pointer;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.BazaarHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Bazaar
{
    public class CModPacketHandler(IBazaarHub bazaarHttpClient, ILogger logger,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        : PacketHandler<CModPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(CModPacket packet, ClientSession clientSession)
        {
            var bzs = await bazaarHttpClient.GetBazaar(packet.BazaarId, null, null, null, null, null, null, null, null);
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
                    });
                    return;
                }

                if (bz.BazaarItem?.Amount == packet.Amount)
                {
                    var patch = new JsonPatch(PatchOperation.Replace(JsonPointer.Parse("/BazaarItem/Price"), packet.NewPrice.AsJsonElement().AsNode()));
                    var bzMod = await bazaarHttpClient.ModifyBazaarAsync(packet.BazaarId, patch);

                    if ((bzMod != null) && (bzMod.BazaarItem?.Price != bz.BazaarItem.Price))
                    {
                        await clientSession.HandlePacketsAsync(new[] { new CSListPacket { Index = 0, Filter = BazaarStatusType.Default } });
                        await clientSession.SendPacketAsync(new SayiPacket
                        {
                            VisualType = VisualType.Player,
                            VisualId = clientSession.Character.CharacterId,
                            Type = SayColorType.Yellow,
                            Message = Game18NConstString.NewSellingPrice,
                            ArgumentType = 4,
                            Game18NArguments = { bzMod.BazaarItem?.Price ?? 0 }
                        });
                        return;
                    }
                }

                await clientSession.SendPacketAsync(new ModaliPacket
                {
                    Type = 1,
                    Message = Game18NConstString.OfferUpdated
                });

            }
            else
            {
                logger.Error(logLanguage[LogLanguageKey.BAZAAR_MOD_ERROR]);
            }
        }
    }
}
