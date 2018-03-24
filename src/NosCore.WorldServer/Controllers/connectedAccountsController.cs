using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NosCore.GameObject;
using System.Linq;

namespace NosCore.WorldServer
{
    [Route("api/[controller]")]
    public class ConnectedAccountsController : Controller
    {
        // GET api/connectedAccounts
        [HttpGet]
        [AllowAnonymous]
        public IEnumerable<string> Get()
        {
            return ServerManager.Instance.Sessions.Select(s=>s.Account.Name);
        }
    }
}
