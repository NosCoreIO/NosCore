//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using NosCore.Shared.Authentication;
using NosCore.Shared.Configuration;

namespace NosCore.Core
{
    public class ConfigureServiceJwtBearerOptions(IOptions<WebApiConfiguration> webApiConfiguration, IHasher hasher)
        : ConfigureJwtBearerOptions(webApiConfiguration, hasher, JwtAudience.Service);
}
