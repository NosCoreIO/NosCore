//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//


using System.Threading.Tasks;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.NpcExchangeService
{
    public interface INpcExchangeService
    {
        Task ExchangeAsync(ClientSession session, NrunRunnerType runner);
    }
}
