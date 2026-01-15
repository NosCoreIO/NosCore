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

using NosCore.Core.I18N;
using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using Serilog;
using System.Threading.Tasks;
using NosCore.GameObject.Services.MapChangeService;

namespace NosCore.PacketHandlers.Command
{
    public class TeleportPacketHandler(ILogger logger, IMapChangeService mapChangeService,
            IGameLanguageLocalizer gameLanguageLocalizer, ISessionRegistry sessionRegistry)
        : PacketHandler<TeleportPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(TeleportPacket teleportPacket, ClientSession session)
        {
            var targetSession =
                sessionRegistry.GetCharacter(s =>
                    s.Name == teleportPacket.TeleportArgument);

            if (!short.TryParse(teleportPacket.TeleportArgument, out var mapId))
            {
                if (targetSession != null)
                {
                    return mapChangeService.ChangeMapInstanceAsync(session, targetSession.MapInstanceId, targetSession.MapX,
                        targetSession.MapY);
                }

                logger.Error(gameLanguageLocalizer[LanguageKey.USER_NOT_CONNECTED,
                    session.Account.Language]);
                return Task.CompletedTask;

            }

            return mapChangeService.ChangeMapAsync(session, mapId, teleportPacket.MapX, teleportPacket.MapY);
        }
    }
}