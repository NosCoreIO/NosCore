using System;
using System.Collections.Generic;
using System.Linq;
using Mapster;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.HttpClients.ConnectedAccountHttpClient;
using NosCore.Data;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Account;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.MasterServer.DataHolders;

namespace NosCore.MasterServer.Controllers
{
    [Route("api/[controller]")]
    [AuthorizeRole(AuthorityType.GameMaster)]
    public class WarehouseController : Controller
    {
        public WarehouseController()
        {
        }

        [HttpGet]
        public bool GetWarehouseItems(long id)
        {
            return false;
        }


        [HttpDelete]
        public bool DeleteWarehouseItem(long id)
        {
            return false;
        }

       

        [HttpPost]
        public bool AddWarehouseItem([FromBody] object mail)
        {
            return false;
        }
    }
}