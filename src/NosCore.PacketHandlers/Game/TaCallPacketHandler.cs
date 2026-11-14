//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.TalentArenaService;
using NosCore.Packets.ServerPackets.Arena;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Game
{
    public class TaCallPacketHandler(ITalentArenaService talentArenaService) : PacketHandler<TaCallPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(TaCallPacket packet, ClientSession session) =>
            talentArenaService.CallAsync(session, packet.CalledIndex);
    }
}
