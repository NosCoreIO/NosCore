using System;
using System.Collections.Concurrent;

namespace NosCore.MasterServer.DataHolders
{
    public class FriendRequestHolder
    {
        public ConcurrentDictionary<Guid, Tuple<long, long>> FriendRequestCharacters { get; set; } =
            new ConcurrentDictionary<Guid, Tuple<long, long>>();
    }
}