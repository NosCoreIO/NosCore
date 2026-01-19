//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.GameObject.Infastructure;

namespace NosCore.GameObject.Services.EventLoaderService
{
    public interface ITimedEventHandler : IEventHandler<Clock, Instant>
    {
    }
}
