//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;

namespace NosCore.GameObject.Messaging.Handlers.Battle
{
    [UsedImplicitly]
    public sealed class DelayInterruptionHandler
    {
        [UsedImplicitly]
        public Task Handle(EntityDamagedEvent evt)
        {
            if (evt.Target is PlayerComponentBundle target)
            {
                target.CancelDelay();
            }

            return Task.CompletedTask;
        }
    }
}
