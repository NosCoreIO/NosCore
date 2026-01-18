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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Services.FriendService
{
    public class FriendRequestRegistry : IFriendRequestRegistry
    {
        private readonly ConcurrentDictionary<Guid, (long SenderId, long ReceiverId)> _requests = new();

        public Guid AddRequest(long senderId, long receiverId)
        {
            var requestId = Guid.NewGuid();
            _requests[requestId] = (senderId, receiverId);
            return requestId;
        }

        public bool TryRemoveRequest(Guid requestId)
        {
            return _requests.TryRemove(requestId, out _);
        }

        public IReadOnlyList<KeyValuePair<Guid, (long SenderId, long ReceiverId)>> FindRequests(long senderId, long receiverId)
        {
            return _requests
                .Where(s => s.Value.SenderId == senderId && s.Value.ReceiverId == receiverId)
                .ToList();
        }
    }
}
