//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.GameObject.Ecs;

namespace NosCore.GameObject.Services.MovementService;

public interface IWalkService
{
    // Charges the step from the character's current cell to (x, y) against its walking
    // allowance. The caller moves the character once this returns true.
    bool TryStep(PlayerComponentBundle character, short x, short y);
}

public sealed class WalkService(IClock clock) : IWalkService
{
    public bool TryStep(PlayerComponentBundle character, short x, short y)
    {
        var map = character.MapInstance.Map;
        if (!map.IsWalkable(x, y))
        {
            return false;
        }

        var now = clock.GetCurrentInstant();
        var elapsed = (now - character.LastMove).TotalMilliseconds;
        var cells = WalkPath.Steps(character.PositionX, character.PositionY, x, y);
        var owed = WalkAllowance.OwedAfter(character.WalkMillisecondsOwed, elapsed,
            WalkAllowance.Cost(cells, character.Speed));
        if (!WalkAllowance.IsAffordable(owed, character.Speed)
            || !WalkPath.Exists(character.PositionX, character.PositionY, x, y, map.IsWalkable))
        {
            return false;
        }

        character.WalkMillisecondsOwed = owed;
        character.LastMove = now;
        return true;
    }
}
