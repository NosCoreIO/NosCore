using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Text;

namespace NosCore.MasterServer
{
    public class FriendRequestHolder
    {
        public ConcurrentDictionary<Guid, Tuple<long, long>> FriendRequestCharacters { get; set; } = new ConcurrentDictionary<Guid, Tuple<long, long>>();
    }
}
