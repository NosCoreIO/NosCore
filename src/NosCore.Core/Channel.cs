//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Shared.Configuration;
using NosCore.Shared.Enumerations;

namespace NosCore.Core
{
    public class Channel
    {
        public WebApiConfiguration? MasterCommunication { get; set; }

        public required string ClientName { get; set; }

        public ServerType ClientType { get; set; }

        public ushort Port { get; set; }

        public byte ServerId { get; set; }

        public long ChannelId { get; set; }

        public string? DisplayHost { get; set; }

        public int? DisplayPort { get; set; }

        public bool StartInMaintenance { get; set; }

        public required string Host { get; set; }

        public int ConnectedAccountLimit { get; set; }
    }
}
