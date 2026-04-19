//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Arch.Core;
using NosCore.Core.I18N;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.PacketHandlerService;
using NosCore.Networking;
using NosCore.Networking.Encoding;
using NosCore.Packets.Attributes;
using NosCore.Packets.Interfaces;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.GameObject.Networking.ClientSession
{
    public class ClientSession(
        ILogger logger,
        IPacketHandlerRegistry packetHandlerRegistry,
        ILogLanguageLocalizer<NosCore.Networking.Resource.LogLanguageKey> networkingLogLanguage,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage,
        IPubSubHub pubSubHub,
        IEncoder encoder,
        IPacketHandlingStrategy packetHandlingStrategy,
        IEnumerable<ISessionDisconnectHandler> disconnectHandlers,
        ISessionRegistry sessionRegistry,
        IGameLanguageLocalizer? gameLanguageLocalizer = null)
        : NetworkClient(logger, networkingLogLanguage, encoder), IPacketSender
    {
        private readonly SemaphoreSlim _handlingPacketLock = new(1, 1);
        private readonly ILogger _logger = logger;
        private PlayerComponentBundle _characterBundle;

        public bool HasPlayerEntity => _characterBundle.Entity != default;

        public ref PlayerComponentBundle Character
        {
            get
            {
                if (_characterBundle.Entity == default)
                {
                    throw new InvalidOperationException("Character entity not initialized");
                }
                return ref _characterBundle;
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

        public Task AcquirePacketLockAsync()
        {
            return _handlingPacketLock.WaitAsync();
        }

        public void ReleasePacketLock()
        {
            _handlingPacketLock.Release();
        }

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
            }
        }

        public void SetPlayerEntity(Entity entity, MapWorld world)
        {
            _characterBundle = new PlayerComponentBundle(entity, world);
            HasSelectedCharacter = true;

            if (Channel != null)
            {
                sessionRegistry.UpdateCharacter(Channel.Id, _characterBundle.CharacterId, _characterBundle.MapInstance?.MapInstanceId ?? Guid.Empty);
            }
        }

        public void ClearPlayerEntity()
        {
            if (_characterBundle.Entity != default && _characterBundle.World != null)
            {
                _characterBundle.World.DestroyEntity(_characterBundle.Entity);
            }
            _characterBundle = default;
            HasSelectedCharacter = false;
        }

        public async Task HandlePacketAsync(NosPackageInfo package)
        {
            try
            {
                await HandlePacketsAsync(package.Packets, true);
            }
            catch (Exception ex)
            {
                _logger.Error(logLanguage[LogLanguageKey.PACKET_HANDLING_ERROR], ex);
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

                foreach (var handler in disconnectHandlers)
                {
                    await handler.HandleDisconnectAsync(this);
                }
            }
            catch (Exception ex)
            {
                _logger.Information(logLanguage[LogLanguageKey.CLIENT_DISCONNECTED], ex);
            }
            finally
            {
                if (Channel != null)
                {
                    sessionRegistry.Unregister(Channel.Id);
                }
                await pubSubHub.UnsubscribeAsync(SessionId);
                _logger.Information(logLanguage[LogLanguageKey.CLIENT_DISCONNECTED]);
            }
        }

        public string GetMessageFromKey(LanguageKey languageKey)
        {
            if (gameLanguageLocalizer == null)
            {
                throw new InvalidOperationException("GameLanguageLocalizer not available in this session type");
            }

            return gameLanguageLocalizer[languageKey, Account.Language];
        }

        public Task HandlePacketsAsync(IEnumerable<IPacket> packetConcatenated, bool isFromNetwork = false)
        {
            return Task.WhenAll(packetConcatenated.Select(packet =>
                packetHandlingStrategy.HandlePacketAsync(packet, this, isFromNetwork)));
        }
    }
}
