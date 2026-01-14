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

using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Core.I18N;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Packets.Attributes;
using NosCore.Packets.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.Networking;
using NosCore.Networking.Encoding;
using NosCore.Networking.SessionRef;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Networking.ClientSession
{
    public class ClientSession : NetworkClient
    {
        private readonly Dictionary<Type, PacketHeaderAttribute> _attributeDic = new();
        private readonly Dictionary<Type, IPacketHandler> _handlersByPacketType = new();
        private readonly SemaphoreSlim _handlingPacketLock = new(1, 1);
        private readonly IPacketHandlingStrategy _packetHandlingStrategy;
        private readonly ILogger _logger;
        private readonly ISessionRefHolder _sessionRefHolder;
        private readonly ILogLanguageLocalizer<LogLanguageKey> _logLanguage;
        private readonly IPubSubHub _pubSubHub;
        private readonly IEnumerable<ISessionDisconnectHandler> _disconnectHandlers;
        private readonly IMapInstanceGeneratorService? _mapInstanceGeneratorService;
        private readonly IMinilandService? _minilandProvider;
        private readonly IGameLanguageLocalizer? _gameLanguageLocalizer;
        private Character? _character;

        public ClientSession(
            ILogger logger,
            IEnumerable<IPacketHandler> packetsHandlers,
            ISessionRefHolder sessionRefHolder,
            ILogLanguageLocalizer<NosCore.Networking.Resource.LogLanguageKey> networkingLogLanguage,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage,
            IPubSubHub pubSubHub,
            IEncoder encoder,
            IPacketHandlingStrategy packetHandlingStrategy,
            IEnumerable<ISessionDisconnectHandler> disconnectHandlers,
            IMinilandService? minilandProvider = null,
            IMapInstanceGeneratorService? mapInstanceGeneratorService = null,
            IGameLanguageLocalizer? gameLanguageLocalizer = null)
            : base(logger, networkingLogLanguage, encoder)
        {
            _logger = logger;
            _sessionRefHolder = sessionRefHolder;
            _logLanguage = logLanguage;
            _pubSubHub = pubSubHub;
            _packetHandlingStrategy = packetHandlingStrategy;
            _disconnectHandlers = disconnectHandlers;
            _minilandProvider = minilandProvider;
            _mapInstanceGeneratorService = mapInstanceGeneratorService;
            _gameLanguageLocalizer = gameLanguageLocalizer;

            foreach (var handler in packetsHandlers)
            {
                var type = handler.GetType().BaseType?.GenericTypeArguments[0]!;
                if (type == null)
                {
                    throw new InvalidOperationException("Packet handler must have a generic type argument");
                }

                if (!_attributeDic.ContainsKey(type))
                {
                    _attributeDic.Add(type, type.GetCustomAttribute<PacketHeaderAttribute>(true)!);
                }

                if (!_handlersByPacketType.ContainsKey(type))
                {
                    _handlersByPacketType.Add(type, handler);
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

        public Character Character
        {
            get
            {
                if (_character != null && HasSelectedCharacter)
                {
                    return _character;
                }

                if (_character == null)
                {
                    _logger.Warning(_logLanguage[LogLanguageKey.CHARACTER_NOT_INIT]);
                    throw new InvalidOperationException("Character not initialized");
                }

                _logger.Warning(_logLanguage[LogLanguageKey.CHARACTER_NOT_INIT]);
                throw new InvalidOperationException("Character not selected");
            }
        }

        public IPacketHandler? GetHandler(Type packetType)
        {
            _handlersByPacketType.TryGetValue(packetType, out var handler);
            return handler;
        }

        public PacketHeaderAttribute? GetPacketAttribute(Type packetType)
        {
            _attributeDic.TryGetValue(packetType, out var attr);
            return attr;
        }

        public int GetSessionIdFromHolder()
        {
            return _sessionRefHolder[SessionKey].SessionId;
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
            Broadcaster.Instance.RegisterSession(this);
        }

        public Task SetCharacterAsync(Character? character)
        {
            _character = character;
            HasSelectedCharacter = character != null;

            if (character == null)
            {
                return Task.CompletedTask;
            }

            character.Session = this;

            if (_minilandProvider != null && _mapInstanceGeneratorService != null)
            {
                return _minilandProvider.InitializeAsync(character, _mapInstanceGeneratorService);
            }

            return Task.CompletedTask;
        }

        public async Task HandlePacketAsync(NosPackageInfo package)
        {
            try
            {
                await HandlePacketsAsync(package.Packets, true).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Error(_logLanguage[LogLanguageKey.PACKET_HANDLING_ERROR], ex);
                await _pubSubHub.UnsubscribeAsync(SessionId);
                await DisconnectAsync();
            }
        }

        public async Task OnDisconnectedAsync()
        {
            try
            {
                if (_character?.IsDisconnecting ?? true)
                {
                    return;
                }

                if (_character != null)
                {
                    _character.IsDisconnecting = true;
                    if (_character.Hp < 1)
                    {
                        _character.Hp = 1;
                    }

                    foreach (var handler in _disconnectHandlers)
                    {
                        await handler.HandleDisconnectAsync(this).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Information(_logLanguage[LogLanguageKey.CLIENT_DISCONNECTED], ex);
            }
            finally
            {
                Broadcaster.Instance.UnregisterSession(this);
                await _pubSubHub.UnsubscribeAsync(SessionId);
                _logger.Information(_logLanguage[LogLanguageKey.CLIENT_DISCONNECTED]);
            }
        }

        public string GetMessageFromKey(LanguageKey languageKey)
        {
            if (_gameLanguageLocalizer == null)
            {
                throw new InvalidOperationException("GameLanguageLocalizer not available in this session type");
            }

            return _gameLanguageLocalizer[languageKey, Account.Language];
        }

        public Task HandlePacketsAsync(IEnumerable<IPacket> packetConcatenated, bool isFromNetwork = false)
        {
            return Task.WhenAll(packetConcatenated.Select(packet =>
                _packetHandlingStrategy.HandlePacketAsync(packet, this, isFromNetwork)));
        }
    }
}
