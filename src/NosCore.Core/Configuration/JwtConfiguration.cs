//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace NosCore.Core.Configuration
{
    public class JwtConfiguration
    {
        [Required]
        [MinLength(RequiredSecrets.JwtSigningKeyMinimumLength)]
        public string SigningKey { get; set; } = null!;

        public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SigningKey));
    }
}
