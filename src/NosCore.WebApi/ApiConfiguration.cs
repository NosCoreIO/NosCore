//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Shared.Configuration;
using System.ComponentModel.DataAnnotations;

namespace NosCore.WebApi
{
    public class ApiConfiguration : ServerConfiguration
    {
        [Required]
        public required WebApiConfiguration MasterCommunication { get; set; }

        [Required]
        public required SqlConnectionConfiguration Database { get; set; }
    }
}
