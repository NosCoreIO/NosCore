//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MateService;
using NosCore.Packets.ClientPackets.Mates;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Mates
{
    public class PtctlPacketHandler(IMateControlService mateControlService)
        : PacketHandler<PtctlPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(PtctlPacket packet, ClientSession session)
        {
            foreach (var order in (packet.Mates ?? []).Take(packet.Amount))
            {
                if (order == null)
                {
                    continue;
                }

                await mateControlService.MoveAsync(session.Character, order.VisualId, order.PositionX, order.PositionY);
            }
        }
    }
}
