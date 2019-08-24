using System;
using System.Collections.Generic;
using System.Linq;
using ChickenAPI.Packets.Enumerations;
using Mapster;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using NosCore.Core;
using NosCore.Data;
using NosCore.Data.Enumerations.Account;
using NosCore.Data.Enumerations.Bazaar;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.WebApi;
using NosCore.MasterServer.DataHolders;

namespace NosCore.MasterServer.Controllers
{
    [Route("api/[controller]")]
    [AuthorizeRole(AuthorityType.GameMaster)]
    public class MailController : Controller
    {
        private readonly IGenericDao<MailDto> _mailDao;
        private readonly IGenericDao<IItemInstanceDto> _itemInstanceDao;

        public MailController(IGenericDao<MailDto> mailDao, IGenericDao<IItemInstanceDto> itemInstanceDao)
        {
            _mailDao = mailDao;
            _itemInstanceDao = itemInstanceDao;
        }

        [HttpGet]
        public List<MailDto> GetMails(long characterId)
        {
            throw new NotImplementedException();
        }


        [HttpDelete]
        public bool DeleteMail(long id)
        {
            throw new NotImplementedException();
        }

        [HttpPatch]
        public bool ViewMail(long id)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        public bool SendMail([FromBody] MailRequest mailRequest)
        {
            throw new NotImplementedException();
        }
    }
}
