//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Shared.Enumerations;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.Core
{
    public static class ChannelNumbering
    {
        // The game names its Act 4 channel 51 wherever a channel number is shown, as in a whisper.
        public const int Act4ChannelNumber = 51;

        public static bool IsListed(this ChannelInfo channel) => channel.Type == ServerType.WorldServer && !channel.IsAct4;

        // The number the channel list gives a world: its place among the listed worlds of its server.
        public static int NumberOf(this IEnumerable<ChannelInfo> channels, ChannelInfo channel) =>
            channel.IsAct4
                ? Act4ChannelNumber
                : 1 + channels.Count(c => c.IsListed() && c.ServerId == channel.ServerId && c.Id < channel.Id);
    }
}
