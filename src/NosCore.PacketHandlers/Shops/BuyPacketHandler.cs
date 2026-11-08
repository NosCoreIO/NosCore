//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Core.I18N;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Shops
{
    public class BuyPacketHandler(ILogger<BuyPacketHandler> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage,
            ISessionRegistry sessionRegistry, IOptions<WorldConfiguration> worldConfiguration,
            IItemGenerationService itemProvider, INpcDialogService npcDialogService)
        : PacketHandler<BuyPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(BuyPacket buyPacket, ClientSession clientSession)
        {
            IAliveEntity? aliveEntity;
            switch (buyPacket.VisualType)
            {
                case VisualType.Player:
                    aliveEntity = sessionRegistry.TryGetCharacter(s => s.VisualId == buyPacket.VisualId, out var buyer) ? buyer : null;
                    break;
                case VisualType.Npc:
                    aliveEntity = clientSession.Character.MapInstance.FindNpc(s => s.VisualId == buyPacket.VisualId);
                    break;

                default:
                    logger.LogError(logLanguage[LogLanguageKey.VISUALTYPE_UNKNOWN],
                        buyPacket.VisualType);
                    return Task.CompletedTask;
            }

            if (aliveEntity != null)
            {
                if (!npcDialogService.IsOpenWith(clientSession.Character, aliveEntity, NpcDialogWindow.Shop))
                {
                    return Task.CompletedTask;
                }

                return clientSession.Character.BuyAsync(aliveEntity.Shop!, buyPacket.Slot, buyPacket.Amount, worldConfiguration, itemProvider);
            }

            logger.LogError(logLanguage[LogLanguageKey.VISUALENTITY_DOES_NOT_EXIST]);
            return Task.CompletedTask;

        }
    }
}
