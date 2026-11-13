//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NodaTime;
using NosCore.GameObject.Infastructure;

namespace NosCore.GameObject.Services.BattleService;

public sealed class CastDelayService : ICastDelayService, ISingletonService
{
    public Task WaitAsync(Duration castTime) => Task.Delay(castTime.ToTimeSpan());
}
