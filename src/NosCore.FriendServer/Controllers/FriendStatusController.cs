//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using ChickenAPI.Packets.ClientPackets.Relations;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.Interfaces;
using ChickenAPI.Packets.ServerPackets.Relations;
using ChickenAPI.Packets.ServerPackets.UI;
using Microsoft.AspNetCore.Mvc;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data;
using NosCore.Data.Enumerations.Account;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using Serilog;

namespace NosCore.FriendServer.Controllers
{
    [Route("api/[controller]")]
    [AuthorizeRole(AuthorityType.GameMaster)]
    public class FriendStatusController : Controller
    {
        private readonly IGenericDao<CharacterRelationDto> _characterRelationDao;
        private readonly ISerializer _packetSerializer;
        private readonly IWebApiAccess _webApiAccess;
        public FriendStatusController(IGenericDao<CharacterRelationDto> characterRelationDao, ISerializer packetSerializer, IWebApiAccess webApiAccess)
        {
            _characterRelationDao = characterRelationDao;
            _packetSerializer = packetSerializer;
            _webApiAccess = webApiAccess;
        }

        [HttpPost]
        public IActionResult SendStatus([FromBody] StatusRequest statusRequest)
        {
            var friendRequest = _characterRelationDao.Where(s => s.CharacterId == statusRequest.CharacterId).ToList();
            foreach (var characterRelation in friendRequest)
            {
                var target = _webApiAccess.GetCharacter(statusRequest.CharacterId, null);
                if (target.Item2 != null)
                {
                    _webApiAccess.BroadcastPacket(new PostedPacket
                    {
                        Packet = _packetSerializer.Serialize(new[]
                        {
                            new FinfoPacket
                            {
                                FriendList = new List<FinfoSubPackets>
                                {
                                    new FinfoSubPackets
                                    {
                                        CharacterId = statusRequest.CharacterId,
                                        IsConnected = statusRequest.Status
                                    }
                                }
                            }
                        }),
                        ReceiverType = ReceiverType.OnlySomeone,
                        SenderCharacter = new Data.WebApi.Character
                        { Id = statusRequest.CharacterId, Name = statusRequest.Name },
                        ReceiverCharacter = target.Item2.ConnectedCharacter
                    });
                }
            }

            return Ok();
        }
    }
}