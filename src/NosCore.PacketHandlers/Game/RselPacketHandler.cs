//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Packets.ClientPackets.Event;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Game
{
    public class RselPacketHandler(ITimeSpaceRewardService timeSpaceRewardService)
        : PacketHandler<RselPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(RselPacket packet, ClientSession session) =>
            timeSpaceRewardService.OpenChestAsync(session);
    }
}
