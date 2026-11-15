//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Messaging.Events
{
    // A buff that hides its bearer landed or ended.
    public sealed record HiddenChangedEvent(IAliveEntity Target)
    {
        public IAliveEntity Target { get => field.Live(); init; } = Target;
    }
}
