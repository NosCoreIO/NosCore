//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MateService;
using NosCore.Packets.ClientPackets.Mates;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Mates
{
    public class UpsPacketHandler(IPartnerSpecialistService partnerSpecialistService)
        : PacketHandler<UpsPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(UpsPacket packet, ClientSession session)
        {
            if (session.Character.SkillTarget(packet.MateTransportId, packet.TargetType, packet.TargetId) is not { } target)
            {
                return Task.CompletedTask;
            }

            return partnerSpecialistService.CastAsync(session.Character, packet.MateTransportId, packet.SkillSlot, target);
        }
    }
}
