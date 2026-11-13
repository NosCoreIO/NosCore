//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Event;
using NosCore.GameObject.Services.IceBreakerService;
using NosCore.GameObject.Services.InstantCombatService;
using NosCore.GameObject.Services.LandOfDeathService;
using NosCore.GameObject.Services.TalentArenaService;
using NodaTime;
using System;

namespace NosCore.GameObject.Services.EventService
{
    public class EventService(IInstantCombatService instantCombatService, IIceBreakerService iceBreakerService,
        ILandOfDeathService landOfDeathService,
        ITalentArenaService talentArenaService) : IEventService
    {
        public Duration CountdownLength(EventType type) => type switch
        {
            EventType.InstantCombat => Duration.FromTimeSpan(InstantCombatService.InstantCombatService.CountdownLength),
            EventType.IceBreaker => Duration.FromTimeSpan(IceBreakerService.IceBreakerService.CountdownLength),
            EventType.LandOfDeath => Duration.Zero,
            EventType.TalentArena => Duration.Zero,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };

        public bool TryStart(EventType type, bool withCountdown) => type switch
        {
            EventType.InstantCombat => instantCombatService.TryStart(withCountdown),
            EventType.IceBreaker => iceBreakerService.TryStart(withCountdown),
            EventType.LandOfDeath => landOfDeathService.TryStart(),
            EventType.TalentArena => talentArenaService.TryStart(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
    }
}
