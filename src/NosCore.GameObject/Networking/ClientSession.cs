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
using NosCore.GameObject.Ecs;
using NosCore.Packets.Attributes;
using NosCore.Packets.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arch.Core;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.CharacterService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.PacketHandlerService;
using NosCore.Networking;
using NosCore.Networking.Encoding;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Networking
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
        private Entity _playerEntity;
        private MapInstance? _currentMapInstance;
        private CharacterGameState? _gameState;
        private CharacterDto? _characterData;

        public bool GameStarted { get; set; }

        public bool MfaValidated { get; set; }

        public int LastKeepAliveIdentity { get; set; }

        public IList<IPacket> WaitForPacketList { get; } = new List<IPacket>();

        public int LastPulse { get; set; }

        public AccountDto Account { get; set; } = null!;

        public int? WaitForPacketsAmount { get; set; }

        public PlayerContext Player
        {
            get
            {
                if (_gameState != null && _characterData != null && _currentMapInstance != null && HasSelectedCharacter)
                {
                    return new PlayerContext(_playerEntity, _currentMapInstance, _gameState, _characterData, Channel);
                }

                _logger.Warning(logLanguage[LogLanguageKey.CHARACTER_NOT_INIT]);
                throw new InvalidOperationException("Player not initialized or selected");
            }
        }

        public bool TryGetPlayer(out PlayerContext player)
        {
            if (_gameState != null && _characterData != null && _currentMapInstance != null && HasSelectedCharacter)
            {
                player = new PlayerContext(_playerEntity, _currentMapInstance, _gameState, _characterData, Channel);
                return true;
            }

            player = default;
            return false;
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

        public Task SetPlayerAsync(CharacterGameState gameState, CharacterDto characterData, MapInstance mapInstance)
        {
            _gameState = gameState;
            _characterData = characterData;
            _currentMapInstance = mapInstance;

            _playerEntity = mapInstance.EcsWorld.CreateCharacter(
                characterData.CharacterId,
                characterData.MapX,
                characterData.MapY,
                0,
                20,
                characterData.Hp,
                0,
                characterData.Mp,
                0,
                characterData.HeroLevel,
                100,
                0, 0, 0, 0,
                characterData.Name,
                characterData.Prefix,
                characterData.AccountId,
                Account.Authority,
                characterData.Level,
                characterData.LevelXp,
                characterData.JobLevel,
                characterData.JobLevelXp,
                characterData.HeroXp,
                characterData.Class,
                characterData.Gender,
                characterData.HairStyle,
                characterData.HairColor,
                characterData.Gold,
                characterData.Reput,
                characterData.Dignity,
                characterData.SpPoint,
                characterData.SpAdditionPoint,
                characterData.Compliment,
                characterData.MapId,
                characterData.MapX,
                characterData.MapY
            );

            HasSelectedCharacter = true;

            if (Channel != null)
            {
                sessionRegistry.UpdatePlayer(Channel.Id, characterData.CharacterId, mapInstance.MapInstanceId, _playerEntity);
            }

            return characterInitializationService.InitializeAsync(Player);
        }

        public void ChangeMapInstance(MapInstance newMapInstance)
        {
            if (_currentMapInstance != null && _characterData != null)
            {
                _currentMapInstance.EcsWorld.DestroyEntity(_playerEntity);
            }

            _currentMapInstance = newMapInstance;

            if (_characterData != null)
            {
                _playerEntity = newMapInstance.EcsWorld.CreateCharacter(
                    _characterData.CharacterId,
                    _characterData.MapX,
                    _characterData.MapY,
                    0,
                    20,
                    _characterData.Hp,
                    0,
                    _characterData.Mp,
                    0,
                    _characterData.HeroLevel,
                    100,
                    0, 0, 0, 0,
                    _characterData.Name,
                    _characterData.Prefix,
                    _characterData.AccountId,
                    Account.Authority,
                    _characterData.Level,
                    _characterData.LevelXp,
                    _characterData.JobLevel,
                    _characterData.JobLevelXp,
                    _characterData.HeroXp,
                    _characterData.Class,
                    _characterData.Gender,
                    _characterData.HairStyle,
                    _characterData.HairColor,
                    _characterData.Gold,
                    _characterData.Reput,
                    _characterData.Dignity,
                    _characterData.SpPoint,
                    _characterData.SpAdditionPoint,
                    _characterData.Compliment,
                    _characterData.MapId,
                    _characterData.MapX,
                    _characterData.MapY
                );

                if (Channel != null)
                {
                    sessionRegistry.UpdatePlayer(Channel.Id, _characterData.CharacterId, newMapInstance.MapInstanceId, _playerEntity);
                }
            }
        }

        public void ClearPlayer()
        {
            if (_currentMapInstance != null)
            {
                _currentMapInstance.EcsWorld.DestroyEntity(_playerEntity);
            }

            _gameState = null;
            _characterData = null;
            _currentMapInstance = null;
            HasSelectedCharacter = false;
        }

        public async Task HandlePacketAsync(NosPackageInfo package)
        {
            try
            {
                await HandlePacketsAsync(package.Packets, true).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Error(logLanguage[LogLanguageKey.PACKET_HANDLING_ERROR], ex);
                if (TryGetPlayer(out var player) && !player.IsDisconnecting)
                {
                    player.IsDisconnecting = true;
                    if (player.Hp < 1)
                    {
                        player.SetHp(1);
                    }

                    foreach (var handler in disconnectHandlers)
                    {
                        try
                        {
                            await handler.HandleDisconnectAsync(this).ConfigureAwait(false);
                        }
                        catch
                        {
                        }
                    }
                }
                if (Channel != null)
                {
                    sessionRegistry.Unregister(Channel.Id);
                }
                await pubSubHub.UnsubscribeAsync(SessionId);
                await DisconnectAsync();
            }
        }

        public async Task OnDisconnectedAsync()
        {
            try
            {
                if (!TryGetPlayer(out var player))
                {
                    return;
                }

                if (player.IsDisconnecting)
                {
                    return;
                }

                player.IsDisconnecting = true;
                if (player.Hp < 1)
                {
                    player.SetHp(1);
                }

                foreach (var handler in disconnectHandlers)
                {
                    await handler.HandleDisconnectAsync(this).ConfigureAwait(false);
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
