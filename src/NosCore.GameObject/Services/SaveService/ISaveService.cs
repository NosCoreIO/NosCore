//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.ComponentEntities.Interfaces;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.SaveService
{
    public interface ISaveService
    {
        Task SaveAsync(ICharacterEntity character);
    }
}
