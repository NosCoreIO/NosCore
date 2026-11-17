//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using JetBrains.Annotations;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;

namespace NosCore.GameObject.Messaging.Handlers.TimeSpace
{
    [UsedImplicitly]
    public sealed class TimeSpaceSucceededHandler
    {
        [UsedImplicitly]
        public void Handle(ScriptedInstanceSucceededEvent evt)
        {
            if (evt.Type == ScriptedInstanceType.TimeSpace)
            {
                evt.Character.ClearTimeSpaceFailures(evt.InstanceId);
            }
        }
    }
}
