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
    public class SayPPacketHandler(IMateControlService mateControlService)
        : PacketHandler<SayPPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(SayPPacket packet, ClientSession session) =>
            mateControlService.TalkAsync(session.Character, packet.PetId, packet.Message);
    }
}
