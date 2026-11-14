//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Packets.ClientPackets.Instance;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Game
{
    public class GitPacketHandler(IMapButtonService mapButtonService) : PacketHandler<GitPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(GitPacket packet, ClientSession session)
        {
            return mapButtonService.UseAsync(session.Character, packet.ButtonId);
        }
    }
}
