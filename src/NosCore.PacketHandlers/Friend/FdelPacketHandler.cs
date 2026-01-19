//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.ClientPackets.Relations;
using NosCore.Packets.ServerPackets.UI;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Friend
{
    public class FdelPacketHandler(IFriendHub friendHttpClient, IChannelHub channelHttpClient,
            IPubSubHub pubSubHub, IGameLanguageLocalizer gameLanguageLocalizer, ISessionRegistry sessionRegistry)
        : PacketHandler<FdelPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(FdelPacket fdelPacket, ClientSession session)
        {
            var list = await friendHttpClient.GetFriendsAsync(session.Character.VisualId);
            var idtorem = list.FirstOrDefault(s => s.CharacterId == fdelPacket.CharacterId);
            if (idtorem != null)
            {
                await friendHttpClient.DeleteAsync(idtorem.CharacterRelationId);
                var targetCharacter = sessionRegistry.GetCharacter(s => s.VisualId == fdelPacket.CharacterId);
                await (targetCharacter == null ? Task.CompletedTask : targetCharacter.SendPacketAsync(await targetCharacter.GenerateFinitAsync(friendHttpClient, channelHttpClient,
                    pubSubHub)));

                await session.Character.SendPacketAsync(await session.Character.GenerateFinitAsync(friendHttpClient, channelHttpClient,
                    pubSubHub));
            }
            else
            {
                await session.SendPacketAsync(new InfoPacket
                {
                    Message = gameLanguageLocalizer[LanguageKey.NOT_IN_FRIENDLIST,
                        session.Account.Language]
                });
            }
        }
    }
}
