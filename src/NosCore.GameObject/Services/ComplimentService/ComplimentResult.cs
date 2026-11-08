//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs;

namespace NosCore.GameObject.Services.ComplimentService;

public readonly record struct ComplimentResult(ComplimentOutcome Outcome, PlayerComponentBundle? Target = null,
    long MinutesToWait = 0);
