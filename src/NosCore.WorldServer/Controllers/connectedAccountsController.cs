using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NosCore.GameObject;
using NosCore.Data;

namespace NosCore.WorldServer
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
