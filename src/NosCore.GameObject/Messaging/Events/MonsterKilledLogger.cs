//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace NosCore.GameObject.Messaging.Events
{
    // Sample handler. Add more by creating any class with a Handle(MonsterKilledEvent) method
    // anywhere in NosCore.GameObject — Wolverine picks them up automatically.
    [UsedImplicitly]
    public sealed class MonsterKilledLogger(ILogger<MonsterKilledLogger> logger)
    {
        [UsedImplicitly]
        public void Handle(MonsterKilledEvent evt)
        {
            logger.LogDebug("Monster {VNum} (id {MapId}) killed by {Killer} on map {Instance}",
                evt.MonsterVNum, evt.MonsterMapId, evt.KillerCharacterId, evt.MapInstanceId);
        }
    }
}
