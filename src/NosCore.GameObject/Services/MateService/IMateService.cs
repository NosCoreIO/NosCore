//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.MateService
{
    public interface IMateService
    {
        Task<List<Mate>> LoadAsync(long characterId);

        Task SaveAsync(IEnumerable<Mate> mates);

        Mate Adopt(MateDto row, NpcMonsterDto npcMonster, IEnumerable<Mate> existing);
    }
}
