//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.ScriptedInstanceService;

namespace NosCore.GameObject.Messaging.Handlers.TimeSpace
{
    [UsedImplicitly]
    public sealed class TimeSpaceSucceededHandler(ITimeSpaceAttemptService timeSpaceAttemptService)
    {
        [UsedImplicitly]
        public Task Handle(ScriptedInstanceSucceededEvent evt) =>
            evt.Type == ScriptedInstanceType.TimeSpace
                ? timeSpaceAttemptService.ResetAsync(evt.Character.CharacterId, evt.InstanceId)
                : Task.CompletedTask;
    }
}
