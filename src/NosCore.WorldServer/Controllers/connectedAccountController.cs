using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NosCore.Core;
using NosCore.Core.Networking;
using NosCore.Data.WebApi;
using NosCore.GameObject.Networking;
using NosCore.Shared.Enumerations.Account;

namespace NosCore.WorldServer.Controllers
{
    [Route("api/[controller]")]
    [AuthorizeRole(AuthorityType.GameMaster)]
    public class ConnectedAccountController : Controller
    {
        // GET api/connectedAccount
        [HttpGet]
        public List<ConnectedAccount> GetconnectedAccount()
        {
            return ServerManager.Instance.Sessions.Values.Select(s =>
                new ConnectedAccount
                {
                    Name = s.Account.Name,
                    Language = s.Account.Language,
                    ChannelId = MasterClientListSingleton.Instance.ChannelId,
                    ConnectedCharacter = s.Character == null ? null : new Character() { Name = s.Character.Name, Id = s.Character.CharacterId },
                }).ToList();
        }
    }
}