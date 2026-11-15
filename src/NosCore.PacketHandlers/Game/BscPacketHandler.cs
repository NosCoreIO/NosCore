//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.RainbowBattleService;
using NosCore.GameObject.Services.TalentArenaService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Arena;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Game
{
    public class BscPacketHandler(ITalentArenaService talentArenaService, IRainbowBattleService rainbowBattleService)
        : PacketHandler<BscPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(BscPacket packet, ClientSession session) => packet.Type switch
        {
            BscPacketType.TalentArena => talentArenaService.CancelAsync(session, packet.Option == 1),
            BscPacketType.ArenaOfMasters => talentArenaService.CancelAsync(session, true),
            BscPacketType.RainbowBattle => rainbowBattleService.CancelAsync(session),
            _ => Task.CompletedTask,
        };
    }
}
