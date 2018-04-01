using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NosCore.GameObject.Networking;

namespace NosCore.WorldServer.Controllers
{
    [Route("api/[controller]")]
    public class ConnectedAccountsController : Controller
    {
        // GET api/connectedAccounts
        [HttpGet]
        [AllowAnonymous]
        public IEnumerable<string> Get()
        {
            return ServerManager.Instance.Sessions.Select(s=>s.Value.Account.Name);
        }
    }
}
