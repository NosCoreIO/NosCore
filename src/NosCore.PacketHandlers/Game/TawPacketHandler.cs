//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.TalentArenaService;
using NosCore.Packets.ClientPackets.Instance;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Game
{
    public class TawPacketHandler(ITalentArenaService talentArenaService) : PacketHandler<TawPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(TawPacket packet, ClientSession session) =>
            talentArenaService.WatchAsync(session, packet.Username);
    }
}
