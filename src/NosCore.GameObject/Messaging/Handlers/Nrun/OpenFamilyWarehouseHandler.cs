//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.FamilyService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    [UsedImplicitly]
    public sealed class OpenFamilyWarehouseHandler(IFamilyWarehouseService familyWarehouseService) : INrunEventHandler
    {
        public NrunRunnerType Runner => NrunRunnerType.OpenFamilyWarehouse;

        public Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet) =>
            familyWarehouseService.OpenAsync(session);
    }
}
