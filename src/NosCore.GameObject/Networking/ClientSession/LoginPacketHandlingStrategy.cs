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
using NosCore.Data.Enumerations.I18N;
using NosCore.Packets.Attributes;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.GameObject.Networking.ClientSession;

public class LoginPacketHandlingStrategy(ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    : IPacketHandlingStrategy
{
    public async Task HandlePacketAsync(IPacket packet, ClientSession session, bool isFromNetwork)
    {
        var packetHeader = packet.Header;
        if (string.IsNullOrWhiteSpace(packetHeader))
        {
            await session.DisconnectAsync().ConfigureAwait(false);
            return;
        }

        var attr = session.GetPacketAttribute(packet.GetType());
        if (attr != null && (attr.Scopes & Scope.OnLoginScreen) == 0)
        {
            logger.Warning(logLanguage[LogLanguageKey.PACKET_USED_WHILE_NOT_ON_LOGIN], packet.Header);
            return;
        }

        var handler = session.GetHandler(packet.GetType());
        if (handler == null)
        {
            logger.Warning(logLanguage[LogLanguageKey.HANDLER_NOT_FOUND], packetHeader);
            return;
        }

        await handler.ExecuteAsync(packet, session).ConfigureAwait(false);
    }
}
