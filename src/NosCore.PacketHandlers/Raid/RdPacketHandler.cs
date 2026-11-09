//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.RaidService;
using NosCore.Packets.ClientPackets.Event;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Raid
{
    public class RdPacketHandler(IRaidService raidService) : PacketHandler<RdPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(RdPacket packet, ClientSession session)
        {
            return (RdRequest)packet.Type switch
            {
                // The leader names who to invite; the invited player answers with the leader's id.
                RdRequest.Join when packet.Parameter == null && session.Character.Group.Raid != null =>
                    raidService.InviteAsync(session, packet.CharacterId),
                RdRequest.Join => raidService.JoinAsync(session, packet.CharacterId),
                RdRequest.Leave => raidService.LeaveAsync(session),
                RdRequest.Kick => raidService.KickAsync(session, packet.CharacterId),
                RdRequest.Disband => raidService.DisbandAsync(session),
                _ => Task.CompletedTask
            };
        }
    }
}
