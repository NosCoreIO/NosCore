//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Networking.SessionGroup;
using System.Collections.Generic;

namespace NosCore.GameObject.Networking.SessionMatchers
{
    public sealed class EveryoneExcept(IReadOnlySet<string> excludedChannelIds) : ISessionMatcher
    {
        public bool Matches(string channelId) => !excludedChannelIds.Contains(channelId);
    }
}
