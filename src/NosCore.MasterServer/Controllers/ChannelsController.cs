using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NosCore.Networking;
using NosCore.Core;

namespace NosCore.MasterServer
{
    [Route("api/[controller]")]
    public class ChannelsController : Controller
    {
        // GET api/channels
        [HttpGet]
        public List<WorldServer> Get()
        {
            return MasterClientListSingleton.Instance.WorldServers;
        } 
    }
}
