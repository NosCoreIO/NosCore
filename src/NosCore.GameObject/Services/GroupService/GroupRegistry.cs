//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
// Copyright (C) 2019 - NosCore
//
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

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
