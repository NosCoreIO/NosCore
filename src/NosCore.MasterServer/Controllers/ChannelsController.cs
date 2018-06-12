using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NosCore.Core;
using NosCore.Core.Networking;

namespace NosCore.MasterServer.Controllers
{
	[Route("api/[controller]")]
	public class ChannelsController : Controller
	{
		// GET api/channels
		[HttpGet]
		[AllowAnonymous]
		public List<WorldServerInfo> Get()
		{
			return MasterClientListSingleton.Instance.WorldServers;
		}
	}
}