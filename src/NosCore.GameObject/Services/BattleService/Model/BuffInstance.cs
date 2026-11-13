//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using NodaTime;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Services.BattleService.Model;

// A buff/debuff currently attached to an entity. CardDto is the design-time description;
// BCards are the resolved stat modifiers applied while the buff is active.
public sealed record BuffInstance(
    short CardId,
    BuffType BuffType,
    IAliveEntity? Caster,
    Instant StartedAt,
    Instant ExpiresAt,
    IReadOnlyList<BCardDto> BCards,
    bool FromSkill = false,
    bool Static = false)
{
    private static readonly Duration PulseStep = Duration.FromSeconds(2);

    // The periodic BCards have fired for every step up to this instant.
    public Instant PulsedUntil { get; init; } = StartedAt;

    // A BCard with ThirdData set fires every ThirdData x 2 seconds while the buff lasts.
    public long PulsesDue(BCardDto bCard, Instant now)
    {
        if (bCard.ThirdData <= 0)
        {
            return 0;
        }

        var interval = PulseStep.BclCompatibleTicks * bCard.ThirdData;
        return (now - StartedAt).BclCompatibleTicks / interval
            - (PulsedUntil - StartedAt).BclCompatibleTicks / interval;
    }

    public bool IsDue(Instant now)
    {
        if (ExpiresAt <= now)
        {
            return true;
        }

        if (FromSkill)
        {
            return false;
        }

        for (var i = 0; i < BCards.Count; i++)
        {
            if (PulsesDue(BCards[i], now) > 0)
            {
                return true;
            }
        }

        return false;
    }
}
