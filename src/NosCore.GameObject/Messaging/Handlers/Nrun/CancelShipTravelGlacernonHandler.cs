//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.Act4ShipService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    [UsedImplicitly]
    public sealed class CancelShipTravelGlacernonHandler(IAct4ShipService act4ShipService) : INrunEventHandler
    {
        public NrunRunnerType Runner => NrunRunnerType.CancelShipTravelGlacernon;

        public bool RequiresNpc => true;

        public Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet) =>
            act4ShipService.DisembarkAsync(session);
    }
}
