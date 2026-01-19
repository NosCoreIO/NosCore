//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.AspNetCore.Authorization;
using NosCore.Shared.Enumerations;
using System;
using System.Linq;

namespace NosCore.Core
{
    public class AuthorizeRoleAttribute : AuthorizeAttribute
    {
        public AuthorizeRoleAttribute(AuthorityType allowedRole)
        {
            var enums = Enum.GetValues(typeof(AuthorityType)).Cast<AuthorityType>().ToList()
                .Where(s => s >= allowedRole);
            Roles = string.Join(",", enums.ToArray());
        }
    }
}
