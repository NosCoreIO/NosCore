//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MateService;
using NosCore.Packets.ClientPackets.Mates;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Mates
{
    public class PsopPacketHandler(IPartnerSpecialistService partnerSpecialistService)
        : PacketHandler<PsopPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(PsopPacket packet, ClientSession session) =>
            partnerSpecialistService.LearnSkillAsync(session.Character, packet.PetSlot, packet.SkillSlot);
    }
}
