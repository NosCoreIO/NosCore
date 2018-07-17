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
		public List<WorldServerInfo> GetChannels()
		{
			return MasterClientListSingleton.Instance.WorldServers;
		}
	}
}