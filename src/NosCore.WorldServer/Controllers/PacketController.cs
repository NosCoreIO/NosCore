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

using Microsoft.AspNetCore.Mvc;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.Packets.Interfaces;
using Serilog;
using System.Threading.Tasks;
using NosCore.Networking;
using NosCore.Shared.I18N;

namespace NosCore.WorldServer.Controllers
{
    [Route("api/[controller]")]
    public class PacketController : Controller
    {
        private readonly IDeserializer _deserializer;
        private readonly ILogger _logger;
        private readonly ILogLanguageLocalizer<LogLanguageKey> _logLanguage;

        public PacketController(ILogger logger, IDeserializer deserializer, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        {
            _logger = logger;
            _logLanguage = logLanguage;
            _deserializer = deserializer;
        }

        // POST api/packet
        [HttpPost]
        public async Task<IActionResult> PostPacketAsync([FromBody] PostedPacket postedPacket)
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
                    _logger.Error(_logLanguage[LogLanguageKey.UNKWNOWN_RECEIVERTYPE]);
                    break;
            }

            return Ok();
        }
    }
}