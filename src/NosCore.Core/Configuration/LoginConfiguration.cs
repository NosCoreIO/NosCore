//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.ClientPackets.Login;
using NosCore.Shared.Configuration;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Core.Configuration
{
    public class LoginConfiguration : ServerConfiguration
    {
        [Required]
        public required WebApiConfiguration MasterCommunication { get; set; }

        [Required]
        public required SqlConnectionConfiguration Database { get; set; }

        public ClientVersionSubPacket? ClientVersion { get; set; }
        public string? Md5String { get; set; }
        public bool EnforceNewAuth { get; set; }
    }
}
