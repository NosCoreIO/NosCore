using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NosCore.Core;
using NosCore.Core.Networking;
using NosCore.Data.WebApi;
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
		public IEnumerable<ConnectedAccount> GetConnectedAccounts()
		{
			return ServerManager.Instance.Sessions.Select(s =>
				new ConnectedAccount() {Name = s.Value.Account.Name, Language = s.Value.Account.Language, ChannelId = MasterClientListSingleton.Instance.ChannelId});
		}
	}
}