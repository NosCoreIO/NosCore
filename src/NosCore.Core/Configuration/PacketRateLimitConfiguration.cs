//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.ComponentModel.DataAnnotations;

namespace NosCore.Core.Configuration
{
    public class PacketRateLimitConfiguration
    {
        [Range(1, 1000)]
        public int PacketsPerSecond { get; set; } = 30;

        [Range(1, 10000)]
        public int Burst { get; set; } = 60;

        // A session keeps dropping packets past this many within the window: it is disconnected.
        [Range(1, 100000)]
        public int DisconnectAfterDroppedPackets { get; set; } = 200;

        [Range(1, 3600)]
        public int DropWindowSeconds { get; set; } = 10;
    }
}
