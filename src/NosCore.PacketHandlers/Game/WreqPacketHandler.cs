//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Packets.ClientPackets.Instance;
using NosCore.Packets.Enumerations;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Game
{
    public class WreqPacketHandler(IScriptedInstanceService scriptedInstanceService,
            ITimeSpaceEntryService timeSpaceEntryService)
        : PacketHandler<WreqPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(WreqPacket packet, ClientSession session)
        {
            var character = session.Character;
            var entrance = scriptedInstanceService.GetNear(character.MapId, character.PositionX, character.PositionY);
            if (entrance == null || entrance.Type != ScriptedInstanceType.TimeSpace)
            {
                return Task.CompletedTask;
            }

            return packet.Value switch
            {
                null or WreqPacketType.DailyTeamTimeSpaceEntry => timeSpaceEntryService.ShowAsync(session, entrance, offerToJoin: packet.Param == null),
                WreqPacketType.TeamTimeSpaceEntry when packet.Param is { } memberId => timeSpaceEntryService.JoinAsync(session, entrance, memberId),
                WreqPacketType.InstanceEntry => timeSpaceEntryService.EnterAsync(session, entrance),
                WreqPacketType.InstanceEntryLoserMode => timeSpaceEntryService.EnterAsync(session, entrance, loserMode: true),
                _ => Task.CompletedTask
            };
        }
    }
}
