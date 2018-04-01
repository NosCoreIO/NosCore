using Microsoft.AspNetCore.Authorization;
using NosCore.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using NosCore.Domain.Account;

namespace NosCore.Core
{
    public class AuthorizeRoleAttribute : AuthorizeAttribute
    {
        public AuthorizeRoleAttribute(AuthorityType allowedRole)
        {
            var allowedRolesAsStrings = string.Empty;
            IEnumerable<AuthorityType> enums = Enum.GetValues(typeof(AuthorityType)).Cast<AuthorityType>().ToList().Where(s => s >= allowedRole);
            Roles = String.Join(",", enums.ToArray());
        }
    }
}
