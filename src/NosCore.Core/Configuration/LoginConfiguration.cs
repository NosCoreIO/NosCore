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
        public WebApiConfiguration MasterCommunication { get; set; } = null!;

        [Required]
        public SqlConnectionConfiguration Database { get; set; } = null!;

        public ClientVersionSubPacket? ClientVersion { get; set; }
        public string? Md5String { get; set; }
        public bool EnforceNewAuth { get; set; }
    }
}
