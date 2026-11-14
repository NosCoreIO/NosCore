//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Messaging.Events
{
    // A buff that changes speed, or stops its bearer from moving or attacking, landed or ended, or
    // the worn gear changed.
    public sealed record MovementConditionChangedEvent(IAliveEntity Target)
    {
        public IAliveEntity Target { get => field.Live(); init; } = Target;
    }
}
