//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Packets.ClientPackets.Miniland;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Miniland
{
    public class MJoinPacketHandler(IFriendHub friendHttpClient, IMinilandService minilandProvider,
            IMapChangeService mapChangeService, ISessionRegistry sessionRegistry)
        : PacketHandler<MJoinPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(MJoinPacket mJoinPacket, ClientSession session)
        {
            var target = sessionRegistry.GetCharacter(s => s.VisualId == mJoinPacket.VisualId);
            var friendList = await friendHttpClient.GetFriendsAsync(session.Character.CharacterId);
            if (target != null && friendList.Any(s => s.CharacterId == mJoinPacket.VisualId))
            {
                var miniland = minilandProvider.GetMiniland(mJoinPacket.VisualId);
                if (miniland.State == MinilandState.Open)
                {
                    await mapChangeService.ChangeMapInstanceAsync(session, miniland.MapInstanceId, 5, 8);
                }
                else
                {
                    if (miniland.State == MinilandState.Private &&
                        friendList.Where(w => w.RelationType != CharacterRelationType.Blocked)
                            .Select(s => s.CharacterId)
                            .ToList()
                            .Contains(target.VisualId))
                    {
                        await mapChangeService.ChangeMapInstanceAsync(session, miniland.MapInstanceId, 5, 8);
                        return;
                    }
                    await session.SendPacketAsync(new InfoiPacket
                    {
                        Message = Game18NConstString.MinilandLocked
                    });
                }
            }
        }
    }
}
