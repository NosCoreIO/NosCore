//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using NosCore.GameObject.Ecs;

namespace NosCore.GameObject.Services.ComplimentService;

public sealed record Complimented(PlayerComponentBundle Target);

public sealed record ComplimentTooSoon(long MinutesToWait);

public union ComplimentResult(Complimented, ComplimentTooSoon, ComplimentOutcome);
