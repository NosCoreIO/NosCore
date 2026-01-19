//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Shared.Enumerations;

namespace NosCore.Core
{
    public class ChannelInfo
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public string Host { get; set; } = null!;
        public ushort Port { get; set; }
        public string? DisplayHost { get; set; }
        public ushort? DisplayPort { get; set; }
        public int ConnectedAccountLimit { get; set; }

        public Instant LastPing { get; set; }

        public ServerType Type { get; set; }
        public bool IsMaintenance { get; set; }
        public byte ServerId { get; set; }
    }
}
