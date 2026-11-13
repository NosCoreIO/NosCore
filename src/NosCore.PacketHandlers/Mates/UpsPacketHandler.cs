//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MateService;
using NosCore.Packets.ClientPackets.Mates;
using NosCore.Shared.Enumerations;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Mates
{
    public class UpsPacketHandler(IPartnerSpecialistService partnerSpecialistService)
        : PacketHandler<UpsPacket>, IWorldPacketHandler
    {
        // A mate counts as an ally of every player and npc, so a monster is the only target it can hit.
        public override Task ExecuteAsync(UpsPacket packet, ClientSession session)
        {
            if (packet.TargetType != VisualType.Monster
                || session.Character.MapInstance.FindMonster(s => s.VisualId == packet.TargetId) is not { } target)
            {
                return Task.CompletedTask;
            }

            return partnerSpecialistService.CastAsync(session.Character, packet.MateTransportId, packet.SkillSlot, target);
        }
    }
}
