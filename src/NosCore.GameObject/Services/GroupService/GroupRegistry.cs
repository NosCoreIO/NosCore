//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Concurrent;
using System.Threading;

namespace NosCore.GameObject.Services.GroupService
{
    public class GroupRegistry : IGroupRegistry
    {
        private readonly ConcurrentDictionary<long, Group> _groupsById = new();
        private readonly ConcurrentDictionary<long, long> _characterToGroupId = new();
        private long _nextId;

        public Group? GetById(long groupId)
        {
            _groupsById.TryGetValue(groupId, out var group);
            return group;
        }

        public void Register(Group group)
        {
            _groupsById[group.GroupId] = group;
        }

        public void Unregister(long groupId)
        {
            _groupsById.TryRemove(groupId, out _);
        }

        public void RegisterMember(long characterId, long groupId)
        {
            _characterToGroupId[characterId] = groupId;
        }

        public void UnregisterMember(long characterId)
        {
            _characterToGroupId.TryRemove(characterId, out _);
        }

        public long GetNextGroupId()
        {
            return Interlocked.Increment(ref _nextId);
        }
    }
}
