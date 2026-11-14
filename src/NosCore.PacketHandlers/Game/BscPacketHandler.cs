//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.TalentArenaService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Arena;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Game
{
    public class BscPacketHandler(ITalentArenaService talentArenaService) : PacketHandler<BscPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(BscPacket packet, ClientSession session) =>
            packet.Type == BscPacketType.TalentArena
                ? talentArenaService.CancelAsync(session, packet.Option == 1)
                : Task.CompletedTask;
    }
}
