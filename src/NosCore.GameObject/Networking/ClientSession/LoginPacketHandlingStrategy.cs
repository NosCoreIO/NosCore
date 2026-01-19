//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.I18N;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Shared.I18N;
using Serilog;
using System.Threading.Tasks;

namespace NosCore.GameObject.Networking.ClientSession;

public class LoginPacketHandlingStrategy(ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    : IPacketHandlingStrategy
{
    public async Task HandlePacketAsync(IPacket packet, ClientSession session, bool isFromNetwork)
    {
        var packetHeader = packet.Header;
        if (string.IsNullOrWhiteSpace(packetHeader))
        {
            await session.DisconnectAsync();
            return;
        }

        var attr = session.GetPacketAttribute(packet.GetType());
        if (attr != null && (attr.Scopes & Scope.OnLoginScreen) == 0)
        {
            logger.Warning(logLanguage[LogLanguageKey.PACKET_USED_WHILE_NOT_ON_LOGIN], packet.Header);
            return;
        }

        var handler = session.GetHandler(packet.GetType());
        if (handler == null)
        {
            logger.Warning(logLanguage[LogLanguageKey.HANDLER_NOT_FOUND], packetHeader);
            return;
        }

        await handler.ExecuteAsync(packet, session);
    }
}
