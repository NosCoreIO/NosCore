//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.Data.Enumerations.Group;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Services.GroupService
{
    public interface IGroupLootService
    {
        GroupSharingType GetSharingType(Group group);

        long AssignOwner(PlayerComponentBundle topDamageDealer, IAliveEntity victim);

        bool CanPickUp(PlayerComponentBundle picker, MapItemComponentBundle mapItem);

        Task ChangeSharingAsync(PlayerComponentBundle player, bool shareAsGroup);
    }
}
