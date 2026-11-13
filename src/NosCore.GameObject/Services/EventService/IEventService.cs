//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Event;
using NodaTime;

namespace NosCore.GameObject.Services.EventService
{
    public interface IEventService
    {
        Duration CountdownLength(EventType type);

        // False when that event is already running on this channel.
        bool TryStart(EventType type, bool withCountdown);
    }
}
