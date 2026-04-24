//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.UpgradeService;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;

namespace NosCore.PacketHandlers.Upgrades
{
    // Dispatches up_gr to whichever IUpgradeOperation declares a matching Kind. Unhandled
    // upgrade types log a warning and no-op — adding a new variant is just registering
    // another IUpgradeOperation with DI.
    public sealed class UpgradePacketHandler(
        IEnumerable<IUpgradeOperation> operations,
        ILogger<UpgradePacketHandler> logger,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        : PacketHandler<UpgradePacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(UpgradePacket packet, ClientSession session)
        {
            var operation = operations.FirstOrDefault(o => o.Kind == packet.UpgradeType);
            if (operation == null)
            {
                logger.LogWarning(logLanguage[LogLanguageKey.UNHANDLED_UPGRADE_TYPE], packet.UpgradeType);
                return;
            }

            var packets = await operation.ExecuteAsync(session, packet).ConfigureAwait(false);
            if (packets.Count > 0)
            {
                await session.SendPacketsAsync(packets).ConfigureAwait(false);
            }
        }
    }
}
