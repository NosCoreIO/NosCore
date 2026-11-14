//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Networking.ClientSession;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.Act4ShipService
{
    public interface IAct4ShipService
    {
        // Opens boarding for the next sailing; false while the last one is still taking passengers.
        bool TryStart();

        Task BoardAsync(ClientSession session);

        Task DisembarkAsync(ClientSession session);
    }
}
