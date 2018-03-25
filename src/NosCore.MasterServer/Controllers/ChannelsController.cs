using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NosCore.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using NosCore.Networking;
using NosCore.Domain;

namespace NosCore.MasterServer
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
