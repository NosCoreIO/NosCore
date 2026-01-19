//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Relations;
using NosCore.Packets.ServerPackets.UI;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Friend
{
    public class BlDelPacketHandler(IBlacklistHub blacklistHttpClient,
            IGameLanguageLocalizer gameLanguageLocalizer)
        : PacketHandler<BlDelPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(BlDelPacket bldelPacket, ClientSession session)
        {
            var list = await blacklistHttpClient.GetBlacklistedAsync(session.Character.VisualId);
            var idtorem = list.FirstOrDefault(s => s.CharacterId == bldelPacket.CharacterId);
            if (idtorem != null)
            {
                await blacklistHttpClient.DeleteAsync(idtorem.CharacterRelationId);
                await session.SendPacketAsync(await session.Character.GenerateBlinitAsync(blacklistHttpClient));
            }
            else
            {
                await session.SendPacketAsync(new InfoPacket
                {
                    Message = gameLanguageLocalizer[LanguageKey.NOT_IN_BLACKLIST,
                        session.Account.Language]
                });
            }
        }
    }
}
