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

using System.Threading.Tasks;
using NosCore.Packets.Interfaces;
using Microsoft.AspNetCore.Mvc;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.Account;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.Group;
using Serilog;

namespace NosCore.WorldServer.Controllers
{
    [Route("api/[controller]")]
    [AuthorizeRole(AuthorityType.GameMaster)]
    public class PacketController : Controller
    {
        private readonly IDeserializer _deserializer;
        private readonly ILogger _logger;

        public PacketController(ILogger logger, IDeserializer deserializer)
        {
            _logger = logger;
            _deserializer = deserializer;
        }

        // POST api/packet
        [HttpPost]
        public async Task<IActionResult> PostPacket([FromBody] PostedPacket postedPacket)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var message = _deserializer.Deserialize(postedPacket.Packet!);

            switch (postedPacket.ReceiverType)
            {
                case ReceiverType.All:
                    await Broadcaster.Instance.SendPacketAsync(message).ConfigureAwait(false);
                    break;
                case ReceiverType.OnlySomeone:
                    ICharacterEntity? receiverSession;

                    if (postedPacket.ReceiverCharacter!.Name != null)
                    {
                        receiverSession = Broadcaster.Instance.GetCharacter(s =>
                            s.Name == postedPacket.ReceiverCharacter.Name);
                    }
                    else
                    {
                        receiverSession = Broadcaster.Instance.GetCharacter(s =>
                            s.VisualId == postedPacket.ReceiverCharacter.Id);
                    }

                    if (receiverSession == null)
                    {
                        return Ok(); //TODO: not found
                    }

                    await receiverSession.SendPacketAsync(message).ConfigureAwait(false);
                    break;
                default:
                    _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.UNKWNOWN_RECEIVERTYPE));
                    break;
            }

            return Ok();
        }
    }
}