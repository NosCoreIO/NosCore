//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Arch.Core;
using NosCore.Core.Concurrency;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.PacketHandlerService;
using NosCore.Networking;
using NosCore.Networking.Encoding;
using NosCore.Packets.Attributes;
using NosCore.Packets.Interfaces;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.GameObject.Networking.ClientSession
{
    public class ClientSession(
        ILogger<ClientSession> logger,
        IPacketHandlerRegistry packetHandlerRegistry,
        ILogLanguageLocalizer<NosCore.Networking.Resource.LogLanguageKey> networkingLogLanguage,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage,
        IPubSubHub pubSubHub,
        IEncoder encoder,
        IPacketHandlingStrategy packetHandlingStrategy,
        IEnumerable<ISessionDisconnectHandler> disconnectHandlers,
        ISessionRegistry sessionRegistry,
        IAuthHub authHub)
        : NetworkClient(logger, networkingLogLanguage, encoder), IPacketSender
    {
        private static readonly AsyncLocal<PacketLockHold?> HeldPacketLocks = new();

        private readonly AsyncLock _handlingPacketLock = new();
        private readonly ILogger<ClientSession> _logger = logger;
        private PlayerSlot? _player;
        private int _changingMap;

        public bool HasPlayerEntity => LivePlayer() != null;

        public ref PlayerComponentBundle Character
        {
            get
            {
                var slot = LivePlayer();
                if (slot == null)
                {
                    throw Volatile.Read(ref _player) == null
                        ? new InvalidOperationException("Character entity not initialized")
                        : new InvalidOperationException("Character entity handle is stale (destroyed or in a disposed world)");
                }

                return ref slot.Bundle;
            }
        }

        // A map change puts the new entity in place before it destroys the old one, both under
        // the worlds' locks. A dead entity in the slot just read therefore means either that the
        // slot has moved on, and is read again, or that the character really is gone.
        private PlayerSlot? LivePlayer()
        {
            while (true)
            {
                var slot = Volatile.Read(ref _player);
                if (slot == null)
                {
                    return null;
                }

                if (slot.Bundle.World != null && slot.Bundle.World.IsAlive(slot.Bundle.Entity))
                {
                    return slot;
                }

                if (ReferenceEquals(Volatile.Read(ref _player), slot))
                {
                    return null;
                }
            }
        }

        public bool GameStarted { get; set; }

        public bool MfaValidated { get; set; }

        public int LastKeepAliveIdentity { get; set; }

        public IList<IPacket> WaitForPacketList { get; } = new List<IPacket>();

        public int LastPulse { get; set; }

        public AccountDto Account { get; set; } = null!;

        public int? WaitForPacketsAmount { get; set; }

        public IPacketHandler? GetHandler(Type packetType)
        {
            return packetHandlerRegistry.GetHandler(packetType);
        }

        public PacketHeaderAttribute? GetPacketAttribute(Type packetType)
        {
            return packetHandlerRegistry.GetPacketAttribute(packetType);
        }

        // Not async on purpose: the hold has to be recorded in the caller's flow, which an
        // async method's own changes to an AsyncLocal never reach.
        public ValueTask<PacketLockScope> AcquirePacketLockAsync(CancellationToken ct = default)
        {
            var parent = HeldPacketLocks.Value;
            while (parent is { Released: true })
            {
                parent = parent.Parent;
            }

            var hold = new PacketLockHold(this, parent);
            HeldPacketLocks.Value = hold;
            return WaitForPacketLockAsync(hold, ct);
        }

        private async ValueTask<PacketLockScope> WaitForPacketLockAsync(PacketLockHold hold, CancellationToken ct)
        {
            try
            {
                return new PacketLockScope(await _handlingPacketLock.AcquireAsync(ct), hold);
            }
            catch
            {
                hold.Released = true;
                throw;
            }
        }

        // Whether the current flow is the one handling this session's packets.
        public bool HoldsPacketLock
        {
            get
            {
                for (var hold = HeldPacketLocks.Value; hold != null; hold = hold.Parent)
                {
                    if (!hold.Released && ReferenceEquals(hold.Session, this))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        // For work that runs both from this session's packet handlers and from elsewhere.
        public ValueTask<PacketLockScope> AcquirePacketLockUnlessHeldAsync() =>
            HoldsPacketLock ? ValueTask.FromResult(default(PacketLockScope)) : AcquirePacketLockAsync();

        // Only one map change at a time per session; the loser of a race simply does nothing.
        public bool TryBeginMapChange() => Interlocked.CompareExchange(ref _changingMap, 1, 0) == 0;

        public void EndMapChange() => Volatile.Write(ref _changingMap, 0);

        public void InitializeAccount(AccountDto accountDto)
        {
            Account = accountDto;
            IsAuthenticated = true;
            if (Channel != null)
            {
                sessionRegistry.Register(new SessionInfo
                {
                    ChannelId = Channel.Id,
                    SessionId = SessionId,
                    Sender = this,
                    AccountName = accountDto.Name,
                    Disconnect = DisconnectAsync
                });

                var remoteAddress = Channel.RemoteAddress;
                if (remoteAddress != null)
                {
                    _ = authHub.RegisterSessionIpAsync(accountDto.Name, remoteAddress);
                }
            }
        }

        public void SetPlayerEntity(Entity entity, MapWorld world)
        {
            var slot = new PlayerSlot(new PlayerComponentBundle(entity, world));
            Volatile.Write(ref _player, slot);
            HasSelectedCharacter = true;

            if (Channel != null)
            {
                sessionRegistry.UpdateCharacter(Channel.Id, slot.Bundle.CharacterId, slot.Bundle.MapInstance?.MapInstanceId ?? Guid.Empty);
            }
        }

        public void ClearPlayerEntity()
        {
            var slot = Interlocked.Exchange(ref _player, null);
            if (slot != null && slot.Bundle.Entity != default && slot.Bundle.World != null)
            {
                slot.Bundle.World.DestroyEntity(slot.Bundle.Entity);
            }
            HasSelectedCharacter = false;
        }

        // Leaves the map through its executor, so the map's own work never sees the entity
        // half gone.
        private async Task ClearPlayerEntityAsync()
        {
            var map = HasPlayerEntity ? Character.MapInstance : null;
            if (map != null)
            {
                try
                {
                    await map.Executor.RunAsync(() =>
                    {
                        ClearPlayerEntity();
                        return Task.CompletedTask;
                    });
                    return;
                }
                catch (ObjectDisposedException)
                {
                }
            }

            ClearPlayerEntity();
        }

        public async Task HandlePacketAsync(NosPackageInfo package)
        {
            try
            {
                await HandlePacketsAsync(package.Packets, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(logLanguage[LogLanguageKey.PACKET_HANDLING_ERROR], ex);
                await pubSubHub.UnsubscribeAsync(SessionId);
                await DisconnectAsync();
            }
        }

        public async Task OnDisconnectedAsync()
        {
            try
            {
                if (!HasSelectedCharacter)
                {
                    return;
                }

                var character = Character;
                if (character.IsDisconnecting)
                {
                    return;
                }

                character.IsDisconnecting = true;

                foreach (var handler in disconnectHandlers
                    .OrderBy(h => h.Order)
                    .ThenBy(h => h.GetType().FullName, StringComparer.Ordinal))
                {
                    await handler.HandleDisconnectAsync(this);
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation(logLanguage[LogLanguageKey.CLIENT_DISCONNECTED], ex);
            }
            finally
            {
                // Destroy the ECS entity so it doesn't linger in the map world as a
                // zombie bundle. Without this the session's Arch.Entity handle stays
                // live in memory — any later lookup produces a bundle whose
                // PlayerStateComponent resolves to default (CharacterDto = null) and
                // NREs on the first property read.
                await ClearPlayerEntityAsync();
                if (Channel != null)
                {
                    sessionRegistry.Unregister(Channel.Id);
                }
                if (Account != null!)
                {
                    try { await authHub.UnregisterSessionIpAsync(Account.Name); }
                    catch (Exception ex) { _logger.LogWarning(ex, "Failed to unregister session IP for {Account}", Account.Name); }
                }
                await pubSubHub.UnsubscribeAsync(SessionId);
                _logger.LogInformation(logLanguage[LogLanguageKey.CLIENT_DISCONNECTED]);
            }
        }

        public Task HandlePacketsAsync(IEnumerable<IPacket> packetConcatenated, bool isFromNetwork = false)
        {
            return Task.WhenAll(packetConcatenated.Select(packet =>
                packetHandlingStrategy.HandlePacketAsync(packet, this, isFromNetwork)));
        }

        private sealed class PlayerSlot(PlayerComponentBundle bundle)
        {
            public PlayerComponentBundle Bundle = bundle;
        }

        public sealed class PacketLockHold(ClientSession session, PacketLockHold? parent)
        {
            public ClientSession Session { get; } = session;

            public PacketLockHold? Parent { get; } = parent;

            // Work started inside the handling inherits the hold; once the handling is over,
            // such work has to take the lock like anyone else.
            public volatile bool Released;
        }

        public readonly struct PacketLockScope(AsyncLock.Releaser releaser, PacketLockHold? hold) : IDisposable
        {
            public void Dispose()
            {
                if (hold == null)
                {
                    return;
                }

                hold.Released = true;
                releaser.Dispose();
            }
        }
    }
}
