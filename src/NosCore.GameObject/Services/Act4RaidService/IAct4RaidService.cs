//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Networking.ClientSession;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.Act4RaidService
{
    public interface IAct4RaidService
    {
        // False when the player is not standing at their side's open raid gate.
        Task<bool> UseGateAsync(ClientSession session);
    }
}
