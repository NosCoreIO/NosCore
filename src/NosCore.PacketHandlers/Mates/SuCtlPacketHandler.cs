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
    public class SuCtlPacketHandler(IMateControlService mateControlService)
        : PacketHandler<SuCtlPacket>, IWorldPacketHandler
    {
        // A mate counts as an ally of every player and npc, so a monster is the only target it can hit.
        public override async Task ExecuteAsync(SuCtlPacket packet, ClientSession session)
        {
            foreach (var order in packet.Mates ?? [])
            {
                if (order is not { TargetType: VisualType.Monster }
                    || session.Character.MapInstance.FindMonster(s => s.VisualId == order.TargetId) is not { } target)
                {
                    continue;
                }

                await mateControlService.AttackAsync(session.Character, order.MateTransportId, target);
            }
        }
    }
}
