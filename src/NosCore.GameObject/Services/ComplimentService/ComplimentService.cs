//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Services.BroadcastService;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Services.ComplimentService;

public interface IComplimentService
{
    ComplimentResult Compliment(PlayerComponentBundle character, long targetCharacterId);
}

public sealed class ComplimentService(IClock clock, ISessionRegistry sessionRegistry, List<MapTypeMapDto> mapTypeMaps)
    : IComplimentService
{
    private const byte MinimumLevel = 31;
    private static readonly Duration DelayAfterConnection = Duration.FromMinutes(60);

    public ComplimentResult Compliment(PlayerComponentBundle character, long targetCharacterId)
    {
        if (targetCharacterId == character.CharacterId
            || !sessionRegistry.TryGetCharacter(s => s.CharacterId == targetCharacterId, out var target))
        {
            return ComplimentOutcome.NoTarget;
        }

        var mapId = character.MapInstance.Map.MapId;
        if (mapTypeMaps.Any(m => m.MapId == mapId
                && m.MapTypeId is (short)MapTypeType.Act4 or (short)MapTypeType.Act42))
        {
            return ComplimentOutcome.InGlacernon;
        }

        if (character.Level < MinimumLevel)
        {
            return ComplimentOutcome.LevelTooLow;
        }

        var now = clock.GetCurrentInstant();
        var availableAt = character.ConnectedAt + DelayAfterConnection;
        if (now < availableAt)
        {
            return new ComplimentTooSoon((long)Math.Ceiling((availableAt - now).TotalMinutes));
        }

        var account = character.Account;
        if (account.LastCompliment is { } last && last.InUtc().Date == now.InUtc().Date)
        {
            return ComplimentOutcome.AlreadyComplimentedToday;
        }

        target.Compliment = (short)(target.Compliment + 1);
        account.LastCompliment = now;
        return new Complimented(target);
    }
}
