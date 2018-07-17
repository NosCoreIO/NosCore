using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NosCore.Core;
using NosCore.GameObject.Networking;
using NosCore.Shared.Enumerations.Account;

namespace NosCore.WorldServer.Controllers
{
    [Route("api/[controller]")]
    class ConnectedCharactersController
    {
        // GET api/connectedCharacters
        [HttpGet]
        [AllowAnonymous]
        public IEnumerable<string> GetConnectedCharacters()
        {
            return ServerManager.Instance.Sessions.Select(s => s.Value.Character.Name);
        }
    }
}
