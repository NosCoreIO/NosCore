//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.MateService
{
    /// <summary>
    /// Reads and writes the mates a character owns.
    /// </summary>
    public interface IMateService
    {
        /// <summary>
        /// Every mate of the character, ready to be talked about: static description attached,
        /// transport id assigned, slots numbered.
        /// </summary>
        Task<List<Mate>> LoadAsync(long characterId);

        /// <summary>Writes the mates back to storage.</summary>
        Task SaveAsync(IEnumerable<Mate> mates);
    }
}
