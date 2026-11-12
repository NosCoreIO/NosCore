//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Services.GroupService;

// Spouses in the same group both carry the couple buff, and lose it as soon as they are not.
public interface ICoupleBuffService
{
    Task RefreshAsync(IEnumerable<ICharacterEntity> characters);
}
