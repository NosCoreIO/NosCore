using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NosCore.Core;
using NosCore.GameObject.Networking;
using NosCore.Shared.Enumerations.Account;

namespace NosCore.WorldServer.Controllers
{
	[Route("api/[controller]")]
	[AuthorizeRole(AuthorityType.GameMaster)]
    public class ConnectedAccountsController : Controller
	{
		// GET api/connectedAccounts
		[HttpGet]
		[AllowAnonymous]
		public IEnumerable<string> GetConnectedAccounts()
		{
			return ServerManager.Instance.Sessions.Select(s => s.Value.Account.Name);
		}
	}
}