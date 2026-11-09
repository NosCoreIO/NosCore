//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.WebApi;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.SubscriberCacheService;

public interface ISubscriberCacheService
{
    Task<IReadOnlyList<Subscriber>> GetSubscribersAsync();
}
