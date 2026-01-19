//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Shared.Configuration;
using System.ComponentModel.DataAnnotations;

namespace NosCore.MasterServer
{
    public class MasterConfiguration : LanguageConfiguration
    {
        [Required]
        public WebApiConfiguration WebApi { get; set; } = null!;

        [Required]
        public SqlConnectionConfiguration Database { get; set; } = null!;
    }
}
