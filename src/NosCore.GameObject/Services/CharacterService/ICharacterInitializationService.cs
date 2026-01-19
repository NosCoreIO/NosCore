//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.ComponentEntities.Entities;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.CharacterService
{
    public interface ICharacterInitializationService
    {
        Task InitializeAsync(Character character);
    }
}
