//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Packets.ClientPackets.Event;
using System;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Game
{
    public class TreqPacketHandler(IScriptedInstanceService scriptedInstanceService,
            ITimeSpaceEntryService timeSpaceEntryService)
        : PacketHandler<TreqPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(TreqPacket packet, ClientSession session)
        {
            var character = session.Character;

            if (Math.Abs(character.PositionX - packet.X) > 1 || Math.Abs(character.PositionY - packet.Y) > 1)
            {
                return Task.CompletedTask;
            }

            var entrance = scriptedInstanceService.GetAt(character.MapId, (short)packet.X, (short)packet.Y);

            if (entrance == null || entrance.Type != ScriptedInstanceType.TimeSpace)
            {
                return Task.CompletedTask;
            }

            return packet.StartPress == 1
                ? timeSpaceEntryService.EnterAsync(session, entrance)
                : timeSpaceEntryService.ShowAsync(session, entrance);
        }
    }
}
