//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Messaging.Events
{
    // Published by CaptureService after a successful pet capture. Intentionally
    // separate from EntityDiedEvent so capture doesn't inherit the kill pipeline's
    // side effects (xp/gold rewards, death bcards, kill-count quest credit).
    // Capture-specific quest objectives and any other capture-only reactions
    // subscribe to this instead.
    public sealed record EntityCapturedEvent(IAliveEntity Victim, IAliveEntity Captor);
}
