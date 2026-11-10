//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Packets.ClientPackets.Instance;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Game
{
    public class EscapePacketHandler(IScriptedInstanceService scriptedInstanceService,
            IMapChangeService mapChangeService)
        : PacketHandler<EscapePacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(EscapePacket packet, ClientSession session)
        {
            var run = scriptedInstanceService.GetRun(session.Character.MapInstanceId);
            return run == null
                ? Task.CompletedTask
                : mapChangeService.ChangeMapAsync(session, run.ReturnMapId, run.ReturnX, run.ReturnY);
        }
    }
}
