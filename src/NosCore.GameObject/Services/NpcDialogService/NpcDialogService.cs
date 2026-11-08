//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.PathFinder.Interfaces;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.NpcDialogService
{
    public class NpcDialogService(IHeuristic distanceCalculator, IClock clock, ISessionRegistry sessionRegistry)
        : INpcDialogService, ISingletonService
    {
        private const int MaxDistance = 5;

        public void Open(PlayerComponentBundle player, IAliveEntity owner, NpcDialogWindow window) =>
            player.NpcDialog = new NpcDialog(window, owner.VisualType, owner.VisualId,
                player.MapInstanceId, clock.GetCurrentInstant());

        public void OpenRemotely(PlayerComponentBundle player, NpcDialogWindow window) =>
            player.NpcDialog = new NpcDialog(window, null, 0,
                player.MapInstanceId, clock.GetCurrentInstant());

        public bool IsWithinReach(PlayerComponentBundle player, IAliveEntity owner) =>
            owner.MapInstanceId == player.MapInstanceId
            && distanceCalculator.GetDistance((player.PositionX, player.PositionY), (owner.PositionX, owner.PositionY))
            <= MaxDistance;

        public bool IsOpenWith(PlayerComponentBundle player, IAliveEntity owner)
        {
            var dialog = player.NpcDialog;
            return dialog != null
                && dialog.OwnerType == owner.VisualType
                && dialog.OwnerId == owner.VisualId
                && dialog.MapInstanceId == player.MapInstanceId
                && IsWithinReach(player, owner);
        }

        public bool IsOpenWith(PlayerComponentBundle player, IAliveEntity owner, NpcDialogWindow window) =>
            player.NpcDialog?.Window == window && IsOpenWith(player, owner);

        public bool IsOpen(PlayerComponentBundle player, NpcDialogWindow window)
        {
            var dialog = player.NpcDialog;
            if (dialog?.Window != window || dialog.MapInstanceId != player.MapInstanceId)
            {
                return false;
            }

            if (dialog.OwnerType == null)
            {
                return true;
            }

            var owner = FindOwner(player, dialog);
            return owner != null && IsWithinReach(player, owner);
        }

        public NpcComponentBundle? FindOpenNpc(PlayerComponentBundle player, NpcDialogWindow window)
        {
            var dialog = player.NpcDialog;
            if (dialog?.Window != window || dialog.OwnerType != VisualType.Npc
                || dialog.MapInstanceId != player.MapInstanceId)
            {
                return null;
            }

            var npc = player.MapInstance.FindNpc(s => s.VisualId == dialog.OwnerId);
            return npc is { } found && IsWithinReach(player, found) ? found : null;
        }

        private IAliveEntity? FindOwner(PlayerComponentBundle player, NpcDialog dialog) =>
            dialog.OwnerType switch
            {
                VisualType.Npc => (IAliveEntity?)player.MapInstance.FindNpc(s => s.VisualId == dialog.OwnerId),
                VisualType.Player => sessionRegistry.TryGetCharacter(s => s.VisualId == dialog.OwnerId, out var owner)
                    ? owner
                    : null,
                _ => null
            };
    }
}
