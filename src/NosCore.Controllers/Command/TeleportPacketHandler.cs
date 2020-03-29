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
using NosCore.Core.I18N;
using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.MapInstanceProvider;
using Serilog;

namespace NosCore.PacketHandlers.Command
{
    public class TeleportPacketHandler : PacketHandler<TeleportPacket>, IWorldPacketHandler
    {
        private readonly ILogger _logger;
        private readonly IMapInstanceProvider _mapInstanceProvider;

        public TeleportPacketHandler(ILogger logger, IMapInstanceProvider mapInstanceProvider)
        {
            _logger = logger;
            _mapInstanceProvider = mapInstanceProvider;
        }

        public override Task ExecuteAsync(TeleportPacket teleportPacket, ClientSession session)
        {
            var targetSession =
                Broadcaster.Instance.GetCharacter(s =>
                    s.Name == teleportPacket.TeleportArgument); //TODO setter to protect

            if (!short.TryParse(teleportPacket.TeleportArgument, out var mapId))
            {
                if (targetSession != null)
                {
                    return session.ChangeMapInstanceAsync(targetSession.MapInstanceId, targetSession.MapX,
                        targetSession.MapY);
                }

                _logger.Error(GameLanguage.Instance.GetMessageFromKey(LanguageKey.USER_NOT_CONNECTED,
                    session.Account.Language));
                return Task.CompletedTask;

            }

            var mapInstance = _mapInstanceProvider.GetBaseMapById(mapId);

            if (mapInstance != null)
            {
                return session.ChangeMapAsync(mapId, teleportPacket.MapX, teleportPacket.MapY);
            }

            _logger.Error(
                GameLanguage.Instance.GetMessageFromKey(LanguageKey.MAP_DONT_EXIST, session.Account.Language));
            return Task.CompletedTask;

        }
    }
}