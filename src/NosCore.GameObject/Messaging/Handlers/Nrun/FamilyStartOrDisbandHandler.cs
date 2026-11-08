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
    public sealed class FamilyStartOrDisbandHandler(IFamilyManagementService familyManagementService) : INrunEventHandler
    {
        public NrunRunnerType Runner => NrunRunnerType.FamilyStartOrDisband;

        public bool RequiresNpc => false;

        public Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet) =>
            packet.Type == 0
                ? familyManagementService.OfferCreationAsync(session)
                : familyManagementService.OfferDisbandAsync(session);
    }
}
