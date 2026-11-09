//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using NosCore.Data.Enumerations.Group;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.PathFinder.Interfaces;

namespace NosCore.GameObject.Services.GroupService
{
    // A drop belongs to its owner for the owner window. In a party the master's looting option
    // decides who else may take it: "Share as group" opens it to every member, "Loot in turn"
    // hands each drop to the next member near the kill.
    public sealed class GroupLootService(IClock clock, IHeuristic distanceCalculator) : IGroupLootService
    {
        private static readonly Duration OwnerWindow = Duration.FromSeconds(30);

        // No client data gives this distance; members further than this from the kill are skipped.
        private const int LootTurnRange = 50;

        public GroupSharingType GetSharingType(Group group) =>
            group.Leader is PlayerComponentBundle { GroupSharing: true }
                ? GroupSharingType.Everyone
                : GroupSharingType.ByOrder;

        public long AssignOwner(PlayerComponentBundle topDamageDealer, IAliveEntity victim)
        {
            var group = topDamageDealer.Group;
            if (!IsParty(group) || GetSharingType(group) == GroupSharingType.Everyone)
            {
                return topDamageDealer.CharacterId;
            }

            var world = victim.MapInstance.EcsWorld;
            var eligible = group.Values
                .OrderBy(s => s.Item1)
                .Select(s => s.Item2)
                .OfType<PlayerComponentBundle>()
                .Where(member => member.World == world && world.IsAlive(member.Handle)
                    && distanceCalculator.GetDistance((member.PositionX, member.PositionY),
                        (victim.PositionX, victim.PositionY)) <= LootTurnRange)
                .ToList();

            return eligible.Count == 0
                ? topDamageDealer.CharacterId
                : eligible[group.NextLootTurn() % eligible.Count].CharacterId;
        }

        public bool CanPickUp(PlayerComponentBundle picker, MapItemComponentBundle mapItem)
        {
            if (mapItem.OwnerId is not { } ownerId || ownerId == picker.CharacterId
                || mapItem.DroppedAt.Plus(OwnerWindow) <= clock.GetCurrentInstant())
            {
                return true;
            }

            var group = picker.Group;
            return IsParty(group) && group.IsMember(ownerId) && GetSharingType(group) == GroupSharingType.Everyone;
        }

        public async Task ChangeSharingAsync(PlayerComponentBundle player, bool shareAsGroup)
        {
            var group = player.Group;
            if (!IsParty(group))
            {
                player.GroupSharing = shareAsGroup;
                await player.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = shareAsGroup ? Game18NConstString.LootShareInGroup : Game18NConstString.LootInTurn
                });
                return;
            }

            if (!group.IsGroupLeader(player.CharacterId))
            {
                await player.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.CanNotChangeGroupMode });
                return;
            }

            player.GroupSharing = shareAsGroup;
            await group.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = shareAsGroup ? Game18NConstString.ShareAsGroupChanged : Game18NConstString.LootInTurnChanged
            });
        }

        private static bool IsParty(Group group) => group.Type == GroupType.Group && group.Count > 1;
    }
}
