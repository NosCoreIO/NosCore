//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Infastructure;
using NosCore.Networking.SessionRef;
using NosCore.Packets.Attributes;
using NosCore.Packets.ClientPackets.Infrastructure;
using NosCore.Packets.ClientPackets.UI;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using MsgiPacket = NosCore.Packets.ServerPackets.UI.MsgiPacket;
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

        await ExecuteHandlerAsync(handler, processedPacket, session, isFromNetwork, attr);
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
                // A launcher client sends thisisgfmode in place of the password.
                Password = session.WaitForPacketList[1].Header!,
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
        if (session.IsAuthenticated && !session.MfaValidated && session.Account?.MfaSecret != null && !isMfa)
        {
            logger.LogWarning("Packet {Header} refused until MFA is validated", packet.Header);
            return false;
        }

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
            commandHeader.Authority > session.Account.Authority)
        {
            return false;
        }

        return true;
    }

    // Runs under the packet lock, so two packets of one batch cannot both pass on a state the
    // first one changes.
    private async Task<bool> ValidateSenderStateAsync(IPacket packet, ClientSession session, PacketHeaderAttribute? attr)
    {
        if (attr == null || !session.HasPlayerEntity)
        {
            return true;
        }

        var character = session.Character;
        if (!character.IsAlive && (attr.Scopes & Scope.WhileDead) == 0 && attr is not CommandPacketHeaderAttribute)
        {
            logger.LogDebug("Packet {Header} refused while dead", packet.Header);
            return false;
        }

        var state = SenderStateOf(character);
        foreach (var refusal in session.GetRefusals(packet.GetType()))
        {
            if ((refusal.States & state) == 0)
            {
                continue;
            }

            logger.LogDebug("Packet {Header} refused while {State}", packet.Header, refusal.States & state);
            if (refusal.Message is { } message)
            {
                await session.SendPacketAsync(new MsgiPacket { Type = MessageType.Default, Message = message });
            }

            return false;
        }

        return true;
    }

    private static SenderState SenderStateOf(PlayerComponentBundle character)
    {
        var state = SenderState.None;
        if (character.IsAlive)
        {
            state |= SenderState.Alive;
        }

        if (character.IsVehicled)
        {
            state |= SenderState.Vehicled;
        }

        if (character.UseSp)
        {
            state |= SenderState.Transformed;
        }

        return state;
    }

    private async Task ExecuteHandlerAsync(IPacketHandler handler, IPacket packet, ClientSession session, bool isFromNetwork, PacketHeaderAttribute? attr)
    {
        if (isFromNetwork)
        {
            using (await session.AcquirePacketLockAsync())
            {
                if (session.IsClosing)
                {
                    return;
                }

                if (!await ValidateSenderStateAsync(packet, session, attr))
                {
                    return;
                }

                await handler.ExecuteAsync(packet, session);
            }
        }
        else
        {
            if (!await ValidateSenderStateAsync(packet, session, attr))
            {
                return;
            }

            await handler.ExecuteAsync(packet, session);
        }
    }
}
