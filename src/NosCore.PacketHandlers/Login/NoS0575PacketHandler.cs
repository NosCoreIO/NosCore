//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.LoginService;
using NosCore.Packets.ClientPackets.Login;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Login
{
    public class NoS0575PacketHandler(ILoginService loginService, IOptions<LoginConfiguration> loginConfiguration,
            ILogger<NoS0575PacketHandler> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        : PacketHandler<NoS0575Packet>, ILoginPacketHandler
    {
        public override Task ExecuteAsync(NoS0575Packet packet, ClientSession clientSession)
        {
            if (!loginConfiguration.Value.EnforceNewAuth)
            {
                return loginService.LoginAsync(packet.Username, packet.Md5String!, packet.ClientVersion!, clientSession,
                    packet.Password!,
                    false, packet.RegionType);
            }

            logger.LogWarning(logLanguage[LogLanguageKey.TRY_OLD_AUTH], packet.Username);
            return Task.CompletedTask;

        }
    }
}
