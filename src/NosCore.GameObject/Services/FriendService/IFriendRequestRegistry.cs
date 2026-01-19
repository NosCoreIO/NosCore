//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;

namespace NosCore.GameObject.Services.FriendService
{
    public interface IFriendRequestRegistry
    {
        IEnumerable<KeyValuePair<Guid, (long SenderId, long ReceiverId)>> GetRequestsForCharacter(long characterId);
        void RegisterRequest(Guid requestId, long senderId, long receiverId);
        bool UnregisterRequest(Guid requestId);
    }
}
