//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Networking.SessionRef;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Packets.ClientPackets.Infrastructure;
using NosCore.Shared.I18N;
using Serilog;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.CharacterScreen
{
    public class DacPacketHandler(IDao<AccountDto, long> accountDao,
            ILogger logger, IAuthHub authHttpClient,
            IPubSubHub pubSubHub, ISessionRefHolder sessionRefHolder,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        : PacketHandler<DacPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(DacPacket packet, ClientSession clientSession)
        {
            await EntryPointPacketHandler.VerifyConnectionAsync(clientSession, logger, authHttpClient,
                accountDao, pubSubHub, true, packet.AccountName, "thisisgfmode", -1, sessionRefHolder, logLanguage);
            if (clientSession.Account == null!)
            {
                return;
            }
            await clientSession.HandlePacketsAsync(new[] { new SelectPacket { Slot = packet.Slot } })
                ;

            logger.Information(logLanguage[LogLanguageKey.ACCOUNT_ARRIVED],
                clientSession.Account.Name);
        }
    }
}
