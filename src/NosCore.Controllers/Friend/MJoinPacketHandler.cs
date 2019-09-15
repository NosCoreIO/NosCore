using ChickenAPI.Packets.ClientPackets.Miniland;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.HttpClients.FriendHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.MinilandProvider;
using System.Linq;

namespace NosCore.PacketHandlers.Friend
{
    public class MJoinPacketHandler : PacketHandler<MJoinPacket>, IWorldPacketHandler
    {
        private readonly IFriendHttpClient _friendHttpClient;
        private readonly IMinilandProvider _minilandProvider;

        public MJoinPacketHandler(IFriendHttpClient friendHttpClient, IMinilandProvider minilandProvider)
        {
            _friendHttpClient = friendHttpClient;
            _minilandProvider = minilandProvider;
        }

        public override void Execute(MJoinPacket mJoinPacket, ClientSession session)
        {
            var target = Broadcaster.Instance.GetCharacter(s => s.VisualId == mJoinPacket.VisualId);
            if (target != null && _friendHttpClient.GetListFriends(session.Character.CharacterId).Any(s => s.CharacterId == mJoinPacket.VisualId))
            {
                var info = _minilandProvider.GetMiniland(mJoinPacket.VisualId);
                if (info.State == MinilandState.Open)
                {
                    session.ChangeMapInstance(info.MapInstanceId, 5, 8);
                }
                else
                {
                    session.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.MINILAND_CLOSED_BY_FRIEND, session.Account.Language)
                    });
                }
            }
        }
    }
}
