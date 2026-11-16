//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Ecs;

namespace NosCore.GameObject.Services.MateService
{
    public interface IMateBlessingService
    {
        /// <summary>Gives the owner the blessing of each team mate out beside them that grants one and they lack.</summary>
        Task BlessAsync(PlayerComponentBundle owner);

        /// <summary>Renews a blessing that ran out while the mate granting it is still out beside its owner.</summary>
        Task RenewAsync(PlayerComponentBundle owner, short cardId);
    }
}
