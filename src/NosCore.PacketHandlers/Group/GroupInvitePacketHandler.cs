//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.ClientPackets.Commands;
using NosCore.Packets.ServerPackets.Groups;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Group
{
    public class GroupInvitePacketHandler(ISessionRegistry sessionRegistry)
        : PacketHandler<GroupInvitePacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(GroupInvitePacket packet, ClientSession session)
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
                new PjoinPacket
                {
                    RequestType = GroupRequestType.Requested,
                    CharacterId = target.VisualId
                }
            });
        }
    }
}
