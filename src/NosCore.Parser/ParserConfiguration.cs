//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Shared.Configuration;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Parser
{
    public class ParserConfiguration : LanguageConfiguration
    {
        [Required]
        public SqlConnectionConfiguration Database { get; set; } = null!;
    }
}
