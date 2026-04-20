//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading;
using System.Threading.Tasks;
using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Services.BattleService.Model;

// One entry in the per-target hit queue. Completion is signalled via Tcs so the enqueuing
// caller can await the outcome even though processing happens on the target's worker.
public sealed record HitRequest(
    IAliveEntity Origin,
    IAliveEntity Target,
    SkillInfo Skill,
    bool IsPrimaryTarget,
    TaskCompletionSource<HitOutcome> Completion,
    CancellationToken Cancellation);
