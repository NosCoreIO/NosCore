//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Services.FriendService
{
    public class FriendRequestRegistry : IFriendRequestRegistry
    {
        private readonly ConcurrentDictionary<Guid, (long SenderId, long ReceiverId)> _friendRequests = new();

        public IEnumerable<KeyValuePair<Guid, (long SenderId, long ReceiverId)>> GetRequestsForCharacter(long characterId) =>
            _friendRequests.Where(s => s.Value.ReceiverId == characterId || s.Value.SenderId == characterId);

        public void RegisterRequest(Guid requestId, long senderId, long receiverId) =>
            _friendRequests[requestId] = (senderId, receiverId);

        public bool UnregisterRequest(Guid requestId) =>
            _friendRequests.TryRemove(requestId, out _);
    }
}
