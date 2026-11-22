//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.Packets.ClientPackets.Battle;

namespace NosCore.PacketHandlers.Battle
{
    // Sent from the Hunting Falcon icon the client shows once Spy Out has set the falcon on a target.
    public class UseIconFalconSkillPacketHandler(IFalconService falconService)
        : PacketHandler<UseIconFalconSkillPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(UseIconFalconSkillPacket packet, ClientSession clientSession) =>
            falconService.StrikeAsync(clientSession.Character);
    }
}
