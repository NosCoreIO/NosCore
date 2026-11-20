//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Networking.ClientSession;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.CharacterService
{
    public interface ICharacterLoadService
    {
        // Puts the character in the world on its base map, with everything stored for it, as the session's player.
        Task<PlayerComponentBundle> LoadAsync(ClientSession session, CharacterDto character);
    }
}
