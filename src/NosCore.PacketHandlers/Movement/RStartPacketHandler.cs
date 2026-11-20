//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MovementService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Packets.ClientPackets.Instance;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Movement
{
    // The answer to the start question of a run's first room: yes starts the run and goes on through
    // the way out, no leaves the player where they stand. A yes that cannot go through yet starts nothing.
    public class RStartPacketHandler(IScriptedInstanceService scriptedInstanceService, IMovementService movementService)
        : PacketHandler<RStartPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(RStartPacket packet, ClientSession session)
        {
            if (packet.Type != RStartPacketType.TimeSpaceInstanceEntry)
            {
                return;
            }

            var (result, wayOut) = movementService.FindPortal(session);
            if (result == PortalResult.TooSoon)
            {
                await session.SendPacketAsync(session.Character.GenerateSayi(SayColorType.Yellow, Game18NConstString.WillMoveShortly));
                return;
            }

            if (result == PortalResult.Ready && wayOut != null && await scriptedInstanceService.ConfirmStartAsync(session, wayOut))
            {
                await movementService.UsePortalAsync(session);
            }
        }
    }
}
