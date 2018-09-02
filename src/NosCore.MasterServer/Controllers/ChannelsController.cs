using System.Collections.Generic;
using System.Linq;
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
        public List<WorldServerInfo> GetChannels(long? id)
        {
            if (id != null) {
                return MasterClientListSingleton.Instance.WorldServers.Where(s=>s.Id == id).ToList();
            }
            return MasterClientListSingleton.Instance.WorldServers;
        }
    }
}