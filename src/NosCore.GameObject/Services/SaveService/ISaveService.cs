//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Networking.ClientSession;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.SaveService
{
    public interface ISaveService
    {
        // False when nothing was stored: the save rolled back and the database keeps the previous state.
        Task<bool> SaveAsync(ClientSession session);
    }
}
