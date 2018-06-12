using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using NosCore.Shared.Enumerations.Account;

namespace NosCore.Core
{
	public class AuthorizeRoleAttribute : AuthorizeAttribute
	{
		public AuthorizeRoleAttribute(AuthorityType allowedRole)
		{
			var allowedRolesAsStrings = string.Empty;
			var enums = Enum.GetValues(typeof(AuthorityType)).Cast<AuthorityType>().ToList()
				.Where(s => s >= allowedRole);
			Roles = string.Join(",", enums.ToArray());
		}
	}
}