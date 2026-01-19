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
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Packets.Attributes;
using NosCore.Packets.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.CharacterService;
using NosCore.GameObject.Services.PacketHandlerService;
using NosCore.Networking;
using NosCore.Networking.Encoding;
using NosCore.Shared.I18N;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.ComponentEntities.Entities;

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
        ICharacterInitializationService characterInitializationService,
        IGameLanguageLocalizer? gameLanguageLocalizer = null)
        : NetworkClient(logger, networkingLogLanguage, encoder), IPacketSender
    {
        private readonly SemaphoreSlim _handlingPacketLock = new(1, 1);
        private readonly ILogger _logger = logger;
        private Character? _character;


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
                    _logger.Warning(logLanguage[LogLanguageKey.CHARACTER_NOT_INIT]);
                    throw new InvalidOperationException("Character not initialized");
                }

                _logger.Warning(logLanguage[LogLanguageKey.CHARACTER_NOT_INIT]);
                throw new InvalidOperationException("Character not selected");
            }
        }

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

        public Task SetCharacterAsync(Character? character)
        {
            _character = character;
            HasSelectedCharacter = character != null;

            if (character == null)
            {
                return Task.CompletedTask;
            }

            character.Account = Account;
            character.Channel = Channel;
            if (Channel != null)
            {
                sessionRegistry.UpdateCharacter(Channel.Id, character.CharacterId, character.MapInstanceId, character);
            }

            return characterInitializationService.InitializeAsync(character);
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

                    foreach (var handler in disconnectHandlers)
                    {
                        await handler.HandleDisconnectAsync(this);
                    }
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
