//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.ClientPackets.Commands;
using NosCore.Packets.ClientPackets.Relations;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Friend
{
    public class FriendInvitePacketHandler(ISessionRegistry sessionRegistry)
        : PacketHandler<FriendInvitePacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(FriendInvitePacket packet, ClientSession session)
        {
            if (!sessionRegistry.TryGetCharacter(s => s.Name == packet.CharacterName, out var target))
            {
                await session.SendPacketAsync(new InfoiPacket
                {
                    Message = Game18NConstString.UnknownCharacter
                });
                return;
            }

            await session.HandlePacketsAsync(new[]
            {
                new FinsPacket
                {
                    CharacterId = target.VisualId,
                    Type = FinsPacketType.Accepted
                }
            });
        }
    }
}
