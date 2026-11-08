//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.ComponentModel.DataAnnotations;

namespace NosCore.WebApi
{
    public class MallConfiguration
    {
        [Required]
        public string SignatureKey { get; set; } = null!;
    }
}
