//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.ScriptedInstanceService;

namespace NosCore.GameObject.Messaging.Handlers.TimeSpace
{
    [UsedImplicitly]
    public sealed class TimeSpaceProgressHandler(IScriptedInstanceService scriptedInstanceService)
    {
        [UsedImplicitly]
        public Task Handle(EntityDiedEvent evt)
        {
            if (evt.Victim is PlayerComponentBundle { MapInstance: { } playerMap } player)
            {
                return scriptedInstanceService.GetRun(playerMap.MapInstanceId) is { } playerRun
                    ? playerRun.MemberDiedAsync(player.CharacterId)
                    : Task.CompletedTask;
            }

            if (evt.Victim is not MonsterComponentBundle { MapInstance: { } mapInstance } monster)
            {
                return Task.CompletedTask;
            }

            var run = scriptedInstanceService.GetRun(mapInstance.MapInstanceId);
            return run == null
                ? Task.CompletedTask
                : run.MonsterKilledAsync(mapInstance.MapInstanceId, monster.VNum);
        }

        [UsedImplicitly]
        public Task Handle(MapInstanceEnteredEvent evt)
        {
            var run = scriptedInstanceService.GetRun(evt.MapInstance.MapInstanceId);
            return run == null
                ? Task.CompletedTask
                : run.EnteredAsync(evt.ClientSession, evt.MapInstance.MapInstanceId);
        }
    }
}
