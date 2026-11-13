//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Event;
using NosCore.GameObject.Services.InstantCombatService;
using NodaTime;
using System;

namespace NosCore.GameObject.Services.EventService
{
    public class EventService(IInstantCombatService instantCombatService) : IEventService
    {
        public Duration CountdownLength(EventType type) => type switch
        {
            EventType.InstantCombat => Duration.FromTimeSpan(InstantCombatService.InstantCombatService.CountdownLength),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };

        public bool TryStart(EventType type, bool withCountdown) => type switch
        {
            EventType.InstantCombat => instantCombatService.TryStart(withCountdown),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
    }
}
