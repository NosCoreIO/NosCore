//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Concurrent;
using NosCore.GameObject.Services.BattleService.Model;

namespace NosCore.GameObject.Ecs.Components;

// Buffs are keyed by CardId so re-applying a card refreshes its duration instead of
// stacking indefinitely. Concurrent so tick / apply / expire can run on different tasks.
public record struct BuffStateComponent(ConcurrentDictionary<short, BuffInstance> ActiveBuffs);
