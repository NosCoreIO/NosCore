//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Infastructure;
using NosCore.Networking.SessionRef;
using NosCore.Packets.Attributes;
using NosCore.Packets.ClientPackets.Infrastructure;
using NosCore.Packets.ClientPackets.UI;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace NosCore.GameObject.Networking.ClientSession;

public class WorldPacketHandlingStrategy(ILogger<WorldPacketHandlingStrategy> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage, ISessionRefHolder sessionRefHolder)
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
            logger.LogWarning(logLanguage[LogLanguageKey.CORRUPT_PACKET], processedPacket);
            await session.DisconnectAsync();
            return;
        }

        var handler = session.GetHandler(processedPacket.GetType());
        if (handler == null)
        {
            logger.LogWarning(logLanguage[LogLanguageKey.HANDLER_NOT_FOUND], packetHeader);
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
            logger.LogError(logLanguage[LogLanguageKey.CORRUPTED_KEEPALIVE], session.SessionId);
            await session.DisconnectAsync();
            return false;
        }

        session.LastKeepAliveIdentity = packet.KeepAliveId ?? 0;

        if (packet.KeepAliveId == null)
        {
            await session.DisconnectAsync();
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

        session.SessionId = sessionRefHolder[session.SessionKey].SessionId;
        logger.LogDebug(logLanguage[LogLanguageKey.CLIENT_ARRIVED], session.SessionId);
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
            logger.LogWarning(logLanguage[LogLanguageKey.PLAYER_IN_SHOP], packet.Header);
            return false;
        }

        var isMfa = packet is GuriPacket guri && guri.Type == GuriPacketType.TextInput && guri.Argument == 3 && guri.VisualId == 0;
        if (!session.HasSelectedCharacter && (attr.Scopes & Scope.OnCharacterScreen) == 0 && !isMfa)
        {
            logger.LogWarning(logLanguage[LogLanguageKey.PACKET_USED_WITHOUT_CHARACTER], packet.Header);
            return false;
        }

        if (session.HasSelectedCharacter && (attr.Scopes & Scope.InGame) == 0)
        {
            logger.LogWarning(logLanguage[LogLanguageKey.PACKET_USED_WHILE_IN_GAME], packet.Header);
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
            using (await session.AcquirePacketLockAsync().ConfigureAwait(false))
            {
                await handler.ExecuteAsync(packet, session).ConfigureAwait(false);
            }
        }
        else
        {
            await handler.ExecuteAsync(packet, session).ConfigureAwait(false);
        }
    }
}
