//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.RaidService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Packets.ClientPackets.Instance;
using NosCore.Packets.Enumerations;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Game
{
    public class RxitPacketHandler(IScriptedInstanceService scriptedInstanceService, IRaidService raidService,
            IMapChangeService mapChangeService)
        : PacketHandler<RxitPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(RxitPacket packet, ClientSession session)
        {
            if (packet.State != RxitPacketType.LeaveInstance)
            {
                return Task.CompletedTask;
            }

            var run = scriptedInstanceService.GetRun(session.Character.MapInstance.MapInstanceId);
            if (run == null)
            {
                return Task.CompletedTask;
            }

            return run.Team != null && run.Team.IsMember(session.Character.CharacterId)
                ? raidService.LeaveAsync(session)
                : mapChangeService.ChangeMapAsync(session, run.ReturnMapId, run.ReturnX, run.ReturnY);
        }
    }
}
