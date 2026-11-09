//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MateService;
using NosCore.Packets.ClientPackets.Mates;
using NosCore.Packets.Enumerations;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Mates
{
    public class PslPacketHandler(IPartnerSpecialistService partnerSpecialistService)
        : PacketHandler<PslPacket>, IWorldPacketHandler
    {
        // The button sends psl 0 either way; psl 1 is what the transformation bar sends when it runs out.
        public override Task ExecuteAsync(PslPacket packet, ClientSession session) =>
            packet.Type == PslPacketType.Wear
                ? partnerSpecialistService.TransformAsync(session.Character)
                : partnerSpecialistService.ToggleAsync(session.Character);
    }
}
