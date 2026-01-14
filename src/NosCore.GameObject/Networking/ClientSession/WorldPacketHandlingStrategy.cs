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
using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations.I18N;
using NosCore.Packets.Attributes;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Packets.ClientPackets.Infrastructure;
using NosCore.Packets.ClientPackets.Login;
using NosCore.Packets.ClientPackets.UI;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.GameObject.Networking.ClientSession;

public class WorldPacketHandlingStrategy(ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    : IPacketHandlingStrategy
{
    public async Task HandlePacketAsync(IPacket packet, ClientSession session, bool isFromNetwork)
    {
        if (isFromNetwork)
        {
            if (await HandleInitialConnectionAsync(packet, session))
            {
                return;
            }

            if (!await ValidateKeepAliveAsync(packet, session))
            {
                return;
            }
        }

        var processedPacket = await HandleEntryPointSequenceAsync(packet, session);
        if (processedPacket == null)
        {
            return;
        }

        var packetHeader = processedPacket.Header;
        if (string.IsNullOrWhiteSpace(packetHeader) && isFromNetwork)
        {
            logger.Warning(logLanguage[LogLanguageKey.CORRUPT_PACKET], processedPacket);
            await session.DisconnectAsync().ConfigureAwait(false);
            return;
        }

        var handler = session.GetHandler(processedPacket.GetType());
        if (handler == null)
        {
            logger.Warning(logLanguage[LogLanguageKey.HANDLER_NOT_FOUND], packetHeader);
            return;
        }

        if (!processedPacket.IsValid)
        {
            return;
        }

        var attr = session.GetPacketAttribute(processedPacket.GetType());
        if (!ValidateScope(processedPacket, session, attr))
        {
            return;
        }

        if (!ValidateAuthority(session, attr))
        {
            return;
        }

        await ExecuteHandlerAsync(handler, processedPacket, session, isFromNetwork);
    }

    private async Task<bool> ValidateKeepAliveAsync(IPacket packet, ClientSession session)
    {
        if (session.LastKeepAliveIdentity != 0 && packet.KeepAliveId != session.LastKeepAliveIdentity + 1)
        {
            logger.Error(logLanguage[LogLanguageKey.CORRUPTED_KEEPALIVE], session.SessionId);
            await session.DisconnectAsync().ConfigureAwait(false);
            return false;
        }

        session.LastKeepAliveIdentity = packet.KeepAliveId ?? 0;

        if (packet.KeepAliveId == null)
        {
            await session.DisconnectAsync().ConfigureAwait(false);
            return false;
        }

        return true;
    }

    private Task<bool> HandleInitialConnectionAsync(IPacket packet, ClientSession session)
    {
        if (session.WaitForPacketsAmount.HasValue || session.LastKeepAliveIdentity != 0)
        {
            return Task.FromResult(false);
        }

        session.SessionId = session.GetSessionIdFromHolder();
        logger.Debug(logLanguage[LogLanguageKey.CLIENT_ARRIVED], session.SessionId);
        session.WaitForPacketsAmount = 2;
        return Task.FromResult(true);
    }

    private Task<IPacket?> HandleEntryPointSequenceAsync(IPacket packet, ClientSession session)
    {
        if (!session.WaitForPacketsAmount.HasValue)
        {
            return Task.FromResult<IPacket?>(packet);
        }

        session.WaitForPacketList.Add(packet);
        var dacIdentification = session.GetPacketAttribute(typeof(DacPacket))?.Identification;

        if (packet.Header != dacIdentification)
        {
            if (session.WaitForPacketList.Count != session.WaitForPacketsAmount)
            {
                session.LastKeepAliveIdentity = packet.KeepAliveId ?? 0;
                return Task.FromResult<IPacket?>(null);
            }

            var entryPointPacket = new EntryPointPacket
            {
                Header = "EntryPoint",
                KeepAliveId = packet.KeepAliveId,
                Name = session.WaitForPacketList[0].Header!,
                Password = "thisisgfmode",
            };

            session.WaitForPacketsAmount = null;
            session.WaitForPacketList.Clear();
            return Task.FromResult<IPacket?>(entryPointPacket);
        }

        session.WaitForPacketsAmount = null;
        session.WaitForPacketList.Clear();
        return Task.FromResult<IPacket?>(packet);
    }

    private bool ValidateScope(IPacket packet, ClientSession session, PacketHeaderAttribute? attr)
    {
        if (attr == null)
        {
            return true;
        }

        if (session.HasSelectedCharacter && (attr.Scopes & Scope.InTrade) == 0 && session.Character.InExchangeOrShop)
        {
            logger.Warning(logLanguage[LogLanguageKey.PLAYER_IN_SHOP], packet.Header);
            return false;
        }

        var isMfa = packet is GuriPacket guri && guri.Type == GuriPacketType.TextInput && guri.Argument == 3 && guri.VisualId == 0;
        if (!session.HasSelectedCharacter && (attr.Scopes & Scope.OnCharacterScreen) == 0 && !isMfa)
        {
            logger.Warning(logLanguage[LogLanguageKey.PACKET_USED_WITHOUT_CHARACTER], packet.Header);
            return false;
        }

        if (session.HasSelectedCharacter && (attr.Scopes & Scope.InGame) == 0)
        {
            logger.Warning(logLanguage[LogLanguageKey.PACKET_USED_WHILE_IN_GAME], packet.Header);
            return false;
        }

        return true;
    }

    private bool ValidateAuthority(ClientSession session, PacketHeaderAttribute? attr)
    {
        if (session.IsAuthenticated && attr is CommandPacketHeaderAttribute commandHeader &&
            (byte)commandHeader.Authority > (byte)session.Account.Authority)
        {
            return false;
        }

        return true;
    }

    private async Task ExecuteHandlerAsync(IPacketHandler handler, IPacket packet, ClientSession session, bool isFromNetwork)
    {
        if (isFromNetwork)
        {
            await session.AcquirePacketLockAsync();
        }

        try
        {
            await Task.WhenAll(handler.ExecuteAsync(packet, session), Task.Delay(200)).ConfigureAwait(false);
        }
        finally
        {
            if (isFromNetwork)
            {
                session.ReleasePacketLock();
            }
        }
    }
}
