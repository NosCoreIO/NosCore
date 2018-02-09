using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OpenNosCore.GameObject;
using OpenNosCore.Data;

namespace OpenNosCore.WorldServer
{
    [Route("api/[controller]")]
    public class connectedAccountsController : Controller
    {
        // GET api/connectedAccounts
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return ServerManager.Instance.Sessions.Select(s=>s.Account.Name);
        }
    }
}
