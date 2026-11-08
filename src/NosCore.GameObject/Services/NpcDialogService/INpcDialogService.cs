//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Services.NpcDialogService
{
    public interface INpcDialogService
    {
        void Open(PlayerComponentBundle player, IAliveEntity owner, NpcDialogWindow window);

        void OpenRemotely(PlayerComponentBundle player, NpcDialogWindow window);

        bool IsWithinReach(PlayerComponentBundle player, IAliveEntity owner);

        // Any window opened by this entity, which must still stand within reach.
        bool IsOpenWith(PlayerComponentBundle player, IAliveEntity owner);

        bool IsOpenWith(PlayerComponentBundle player, IAliveEntity owner, NpcDialogWindow window);

        // The window, opened by an item or by an owner still within reach.
        bool IsOpen(PlayerComponentBundle player, NpcDialogWindow window);

        NpcComponentBundle? FindOpenNpc(PlayerComponentBundle player, NpcDialogWindow window);
    }
}
