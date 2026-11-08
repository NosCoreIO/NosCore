//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.BazaarHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Bazaar
{
    public class CModPacketHandler(IBazaarHub bazaarHttpClient, ILogger<CModPacketHandler> logger,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage,
            INpcDialogService npcDialogService)
        : PacketHandler<CModPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(CModPacket packet, ClientSession clientSession)
        {
            if (!npcDialogService.IsOpen(clientSession.Character, NpcDialogWindow.Bazaar))
            {
                return;
            }

            var bzs = await bazaarHttpClient.GetBazaar(packet.BazaarId, null, null, null, null, null, null, null, null);
            var bz = bzs.FirstOrDefault();
            if ((bz != null) && (bz.BazaarItem?.SellerId == clientSession.Character.CharacterId) &&
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
                    BazaarLink? bzMod;
                    try
                    {
                        var operationId = Guid.NewGuid();
                        bzMod = await BazaarHubOperations.RequestAsync(() => bazaarHttpClient.ModifyBazaarAsync(
                            operationId, packet.BazaarId, clientSession.Character.CharacterId, packet.NewPrice));
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, logLanguage[LogLanguageKey.BAZAAR_MOD_ERROR]);
                        bzMod = null;
                    }

                    if ((bzMod != null) && (bzMod.BazaarItem?.Price == packet.NewPrice))
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
                logger.LogError(logLanguage[LogLanguageKey.BAZAAR_MOD_ERROR]);
            }
        }
    }
}
