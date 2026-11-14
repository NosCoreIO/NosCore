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
    public class MkRaidPacketHandler(IRaidService raidService) : PacketHandler<MkRaidPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(MkRaidPacket packet, ClientSession session) =>
            raidService.StartAsync(session);
    }
}
