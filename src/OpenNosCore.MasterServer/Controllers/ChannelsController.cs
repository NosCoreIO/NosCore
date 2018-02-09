using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OpenNosCore.Networking;
using OpenNosCore.Core;

namespace OpenNosCore.MasterServer
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
