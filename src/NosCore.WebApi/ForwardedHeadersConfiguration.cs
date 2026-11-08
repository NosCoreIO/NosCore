//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;

namespace NosCore.WebApi
{
    public class ForwardedHeadersConfiguration
    {
        public List<string> KnownProxies { get; set; } = new();

        public List<string> KnownNetworks { get; set; } = new();
    }
}
