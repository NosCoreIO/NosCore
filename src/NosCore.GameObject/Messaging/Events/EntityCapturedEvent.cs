//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Messaging.Events
{
    // Raised by CaptureService after a successful pet capture. Intentionally
    // separate from EntityDiedEvent so capture doesn't inherit the kill pipeline's
    // side effects (xp/gold rewards, death bcards, kill-count quest credit).
    // Capture-specific quest objectives and any other capture-only reactions
    // subscribe to this instead.
    // BecamePet is false when a CaptureWithoutGettingTheMonster quest took the monster instead.
    public sealed record EntityCapturedEvent(IAliveEntity Victim, IAliveEntity Captor, bool BecamePet) : IGameEvent
    {
        public IAliveEntity? Subject => Victim;

        // Read when handled, possibly after a map change replaced a player's entity.
        public IAliveEntity Victim { get => field.Live(); init; } = Victim;

        public IAliveEntity Captor { get => field.Live(); init; } = Captor;
    }
}
