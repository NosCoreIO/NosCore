//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.MateService;

namespace NosCore.GameObject.Messaging.Handlers.Mate
{
    // A team pet comes along onto every map, and a caught one may join the team where it was caught.
    [UsedImplicitly]
    public sealed class PetSkillBarHandler(IPetSkillBarService petSkillBarService)
    {
        [UsedImplicitly]
        public Task Handle(MapInstanceEnteredEvent evt) => petSkillBarService.RefreshAsync(evt.ClientSession.Character);

        [UsedImplicitly]
        public Task Handle(EntityCapturedEvent evt) =>
            evt.Captor is PlayerComponentBundle owner ? petSkillBarService.RefreshAsync(owner) : Task.CompletedTask;
    }
}
