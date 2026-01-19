//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Relations;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.I18N;
using Serilog;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Friend
{
    public class BlInsPackettHandler(IBlacklistHub blacklistHttpClient, ILogger logger,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        : PacketHandler<BlInsPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(BlInsPacket blinsPacket, ClientSession session)
        {
            var result = await blacklistHttpClient.AddBlacklistAsync(new BlacklistRequest
            { CharacterId = session.Character.CharacterId, BlInsPacket = blinsPacket });
            switch (result)
            {
                case LanguageKey.CANT_BLOCK_FRIEND:
                    await session.SendPacketAsync(new InfoiPacket
                    {
                        Message = Game18NConstString.CannotBlackListFriend
                    });
                    break;
                case LanguageKey.ALREADY_BLACKLISTED:
                    await session.SendPacketAsync(new InfoiPacket
                    {
                        Message = Game18NConstString.AlreadyBlacklisted
                    });
                    break;
                case LanguageKey.BLACKLIST_ADDED:
                    await session.SendPacketAsync(new InfoiPacket
                    {
                        Message = Game18NConstString.CharacterBlacklisted
                    });
                    await session.SendPacketAsync(await session.Character.GenerateBlinitAsync(blacklistHttpClient));
                    break;
                default:
                    logger.Warning(logLanguage[LogLanguageKey.FRIEND_REQUEST_DISCONNECTED]);
                    break;
            }
        }
    }
}
