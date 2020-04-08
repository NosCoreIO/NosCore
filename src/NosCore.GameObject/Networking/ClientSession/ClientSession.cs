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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NosCore.Packets.Attributes;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Map;
using DotNetty.Transport.Channels;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data;
using NosCore.Data.CommandPackets;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Account;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.HttpClients.FriendHttpClient;
using NosCore.GameObject.HttpClients.PacketHttpClient;
using NosCore.GameObject.Networking.ChannelMatcher;
using NosCore.GameObject.Networking.Group;
using NosCore.GameObject.Providers.ExchangeProvider;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.GameObject.Providers.MapInstanceProvider;
using NosCore.GameObject.Providers.MinilandProvider;
using NosCore.Packets.ServerPackets.Quest;
using Serilog;

namespace NosCore.GameObject.Networking.ClientSession
{
    public class ClientSession : NetworkClient, IClientSession
    {
        private readonly Dictionary<Type, PacketHeaderAttribute> _attributeDic =
            new Dictionary<Type, PacketHeaderAttribute>();

        private readonly IExchangeProvider _exchangeProvider = null!;
        private readonly IFriendHttpClient _friendHttpClient = null!;

        private readonly bool _isWorldClient;
        private readonly ILogger _logger;
        private readonly IMapInstanceProvider _mapInstanceProvider = null!;
        private readonly IMinilandProvider _minilandProvider = null!;
        private readonly IPacketHttpClient _packetHttpClient;
        private readonly ISerializer _packetSerializer;
        private readonly IEnumerable<IPacketHandler> _packetsHandlers;
        private Character? _character;
        private int? _waitForPacketsAmount;

        public ClientSession(ServerConfiguration configuration,
            ILogger logger, IEnumerable<IPacketHandler> packetsHandlers, IFriendHttpClient friendHttpClient,
            ISerializer packetSerializer, IPacketHttpClient packetHttpClient)
            : this(configuration, null, null, logger, packetsHandlers, friendHttpClient, packetSerializer,
                packetHttpClient, null)
        {
        }

        public ClientSession(ServerConfiguration configuration, IMapInstanceProvider? mapInstanceProvider,
            IExchangeProvider? exchangeProvider, ILogger logger,
            IEnumerable<IPacketHandler> packetsHandlers, IFriendHttpClient friendHttpClient,
            ISerializer packetSerializer, IPacketHttpClient packetHttpClient,
            IMinilandProvider? minilandProvider) : base(logger)
        {
            _logger = logger;
            _packetsHandlers = packetsHandlers.ToList();
            _friendHttpClient = friendHttpClient;
            _packetSerializer = packetSerializer;
            _packetHttpClient = packetHttpClient;
            if (!(configuration is WorldConfiguration worldConfiguration))
            {
                return;
            }

            WorldConfiguration = worldConfiguration;
            _mapInstanceProvider = mapInstanceProvider!;
            _exchangeProvider = exchangeProvider!;
            _minilandProvider = minilandProvider!;
            _isWorldClient = true;
            foreach (var handler in _packetsHandlers)
            {
                var type = handler.GetType().BaseType?.GenericTypeArguments[0]!;
                if (!_attributeDic.ContainsKey(type ?? throw new InvalidOperationException()))
                {
                    _attributeDic.Add(type, type.GetCustomAttribute<PacketHeaderAttribute>(true)!);
                }
            }
        }

        public WorldConfiguration WorldConfiguration { get; } = null!;

        public bool GameStarted { get; set; }

        public int LastKeepAliveIdentity { get; set; }

        public IList<IPacket> WaitForPacketList { get; } = new List<IPacket>();

        public int LastPulse { get; set; }

        public AccountDto Account { get; set; } = null!;

        public Character Character
        {
            get
            {
                if ((_character == null) || HasSelectedCharacter)
                {
                    return _character!;
                }

                // cant access an
                _logger.Warning(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CHARACTER_NOT_INIT));
                throw new NullReferenceException();

            }

            private set => _character = value;
        }

        public void InitializeAccount(AccountDto accountDto)
        {
            Account = accountDto;
            IsAuthenticated = true;
            Broadcaster.Instance.RegisterSession(this);
        }

        public void SetCharacter(Character? character)
        {
            _character = character;
            HasSelectedCharacter = character != null;
            if (character == null)
            {
                return;
            }

            Character.Session = this;
            _minilandProvider.Initialize(character);
        }

        public override async void ChannelRead(IChannelHandlerContext context, object message)
        {
            if (!(message is IEnumerable<IPacket> buff))
            {
                return;
            }

            await HandlePacketsAsync(buff, context).ConfigureAwait(false);
        }

        public override async void ChannelUnregistered(IChannelHandlerContext context)
        {
            try
            {
                if (_character?.IsDisconnecting ?? true)
                {
                    return;
                }

                if (_character != null)
                {
                    Character.IsDisconnecting = true;
                    if (Character.Hp < 1)
                    {
                        Character.Hp = 1;
                    }

                    await Character.SendFinfoAsync(_friendHttpClient, _packetHttpClient, _packetSerializer, false).ConfigureAwait(false);

                    var targetId = _exchangeProvider.GetTargetId(Character.VisualId);
                    if (targetId.HasValue)
                    {
                        var closeExchange =
                            _exchangeProvider.CloseExchange(Character.VisualId, ExchangeResultType.Failure);
                        if (Broadcaster.Instance.GetCharacter(s => s.VisualId == targetId) is Character target)
                        {
                            await target.SendPacketAsync(closeExchange).ConfigureAwait(false);
                        }
                    }

                    await Character.LeaveGroupAsync().ConfigureAwait(false);
                    await Character.MapInstance.SendPacketAsync(Character.GenerateOut()).ConfigureAwait(false);
                    Character.SaveAsync();

                    _minilandProvider.DeleteMinilandAsync(Character.CharacterId);
                }

                Broadcaster.Instance.UnregisterSession(this);
                _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CLIENT_DISCONNECTED));
            }
            catch
            {
                _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CLIENT_DISCONNECTED));
            }
        }

        public Task ChangeMapAsync()
        {
            return ChangeMapAsync(null, null, null);
        }

        public async Task ChangeMapAsync(short? mapId, short? mapX, short? mapY)
        {
            if (Character == null)
            {
                return;
            }

            if (mapId != null)
            {
                Character.MapInstanceId = _mapInstanceProvider.GetBaseMapInstanceIdByMapId((short)mapId);
            }

            try
            {
                _mapInstanceProvider.GetMapInstance(Character.MapInstanceId);
            }
            catch
            {
                return;
            }

            await ChangeMapInstanceAsync(Character.MapInstanceId, mapX, mapY).ConfigureAwait(false);
        }

        public Task ChangeMapInstanceAsync(Guid mapInstanceId)
        {
            return ChangeMapInstanceAsync(mapInstanceId, null, null);
        }

        public async Task ChangeMapInstanceAsync(Guid mapInstanceId, int? mapX, int? mapY)
        {
            if ((Character?.MapInstance == null) || Character.IsChangingMapInstance)
            {
                return;
            }

            try
            {
                Character.IsChangingMapInstance = true;

                if (Channel?.Id != null)
                {
                    Character.MapInstance.Sessions.Remove(Channel);
                }

                Character.MapInstance.LastUnregister = SystemTime.Now();
                await LeaveMapAsync().ConfigureAwait(false);
                if (Character.MapInstance.Sessions.Count == 0)
                {
                    Character.MapInstance.IsSleeping = true;
                }

                if (Character.IsSitting)
                {
                    Character.IsSitting = false;
                }

                Character.MapInstanceId = mapInstanceId;
                Character.MapInstance = _mapInstanceProvider.GetMapInstance(mapInstanceId)!;

                if (Character.MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance)
                {
                    Character.MapId = Character.MapInstance.Map.MapId;
                    if ((mapX != null) && (mapY != null))
                    {
                        Character.MapX = (short)mapX;
                        Character.MapY = (short)mapY;
                    }
                }

                if ((mapX != null) && (mapY != null))
                {
                    Character.PositionX = (short)mapX;
                    Character.PositionY = (short)mapY;
                }

                await SendPacketAsync(Character.GenerateCInfo()).ConfigureAwait(false);
                await SendPacketAsync(Character.GenerateCMode()).ConfigureAwait(false);
                await SendPacketAsync(Character.GenerateEq()).ConfigureAwait(false);
                await SendPacketAsync(Character.GenerateEquipment()).ConfigureAwait(false);
                await SendPacketAsync(Character.GenerateLev()).ConfigureAwait(false);
                await SendPacketAsync(Character.GenerateStat()).ConfigureAwait(false);
                await SendPacketAsync(Character.GenerateAt()).ConfigureAwait(false);
                await SendPacketAsync(Character.GenerateCond()).ConfigureAwait(false);
                await SendPacketAsync(Character.MapInstance.GenerateCMap()).ConfigureAwait(false);
                await SendPacketAsync(Character.GeneratePairy(
                    Character.InventoryService!.LoadBySlotAndType((byte)EquipmentType.Fairy,
                        NoscorePocketType.Wear)?.ItemInstance as WearableInstance)).ConfigureAwait(false);
                await SendPacketsAsync(Character.MapInstance.GetMapItems(Character.AccountLanguage)).ConfigureAwait(false);
                await SendPacketsAsync(Character.MapInstance.MapDesignObjects.Values.Select(mp => mp.GenerateEffect())).ConfigureAwait(false);

                var minilandPortals = _minilandProvider
                    .GetMinilandPortals(Character.CharacterId)
                    .Where(s => s.SourceMapInstanceId == mapInstanceId)
                    .ToList();

                if (minilandPortals.Count > 0)
                {
                    await SendPacketsAsync(minilandPortals.Select(s => s.GenerateGp())).ConfigureAwait(false);
                }

                await SendPacketAsync(Character.Group!.GeneratePinit()).ConfigureAwait(false);
                if (!Character.Group.IsEmpty)
                {
                    await SendPacketsAsync(Character.Group.GeneratePst()).ConfigureAwait(false);
                }

                if ((Character.Group.Type == GroupType.Group) && (Character.Group.Count > 1))
                {
                    await Character.MapInstance.SendPacketAsync(Character.Group.GeneratePidx(Character)).ConfigureAwait(false);
                }

                var mapSessions = Broadcaster.Instance.GetCharacters(s =>
                    (s != Character) && (s.MapInstance.MapInstanceId == Character.MapInstanceId));

                Parallel.ForEach(mapSessions, s =>
                {
                    await SendPacketAsync(s.GenerateIn(s.Authority == AuthorityType.Moderator
                        ? $"[{GameLanguage.Instance.GetMessageFromKey(LanguageKey.SUPPORT, s.AccountLanguage)}]"
                        : string.Empty));
                    if (s.Shop == null)
                    {
                        return;
                    }

                    await SendPacketAsync(s.GeneratePFlag());
                    await SendPacketAsync(s.GenerateShop(Account.Language));
                });
                await Character.SendPacketsAsync(Character.Quests.Values.Where(q => q.Quest.TargetMap == Character.MapId)
                    .Select(qst => qst.Quest.GenerateTargetPacket())).ConfigureAwait(false);
                await Character.MapInstance.SendPacketAsync(Character.GenerateTitInfo()).ConfigureAwait(false);
                Character.MapInstance.IsSleeping = false;
                if (Channel?.Id != null)
                {
                    if (!Character.Invisible)
                    {
                        await Character.MapInstance.SendPacketAsync(Character.GenerateIn(Character.Authority == AuthorityType.Moderator
                                ? $"[{Character.Session.GetMessageFromKey(LanguageKey.SUPPORT)}]" : string.Empty),
                            new EveryoneBut(Character.Channel!.Id)).ConfigureAwait(false);
                    }

                    Character.MapInstance.Sessions.Add(Channel);
                }

                Character.MapInstance.Requests[MapInstanceEventType.Entrance]
                    .OnNext(new RequestData<MapInstance>(Character.Session, Character.MapInstance));

                Character.IsChangingMapInstance = false;
            }
            catch (Exception)
            {
                _logger.Warning(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.ERROR_CHANGE_MAP));
                Character.IsChangingMapInstance = false;
            }
        }

        private async Task LeaveMapAsync()
        {
            await SendPacketAsync(new MapOutPacket()).ConfigureAwait(false);
            await Character.MapInstance.SendPacketAsync(Character.GenerateOut(),
                new EveryoneBut(Channel!.Id)).ConfigureAwait(false);
        }

        public string GetMessageFromKey(LanguageKey languageKey)
        {
            return GameLanguage.Instance.GetMessageFromKey(languageKey, Account.Language);
        }

        public Task HandlePacketsAsync(IEnumerable<IPacket> packetConcatenated, IChannelHandlerContext? contex = null)
        {
            return Task.WhenAll(packetConcatenated.Select(async pack =>
            {
                var packet = pack;
                if (_isWorldClient)
                {
                    if (contex != null)
                    {
                        if ((LastKeepAliveIdentity != 0) && (packet.KeepAliveId != LastKeepAliveIdentity + 1))
                        {
                            _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CORRUPTED_KEEPALIVE),
                                SessionId);
                            await DisconnectAsync().ConfigureAwait(false);
                            return;
                        }

                        if (!_waitForPacketsAmount.HasValue && (LastKeepAliveIdentity == 0))
                        {
                            SessionId = SessionFactory.Instance.Sessions[contex.Channel.Id.AsLongText()].SessionId;
                            _logger.Debug(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CLIENT_ARRIVED),
                                SessionId);
                            _waitForPacketsAmount = 2;
                            return;
                        }

                        LastKeepAliveIdentity = packet.KeepAliveId ?? 0;

                        if (packet.KeepAliveId == null)
                        {
                            await DisconnectAsync().ConfigureAwait(false);
                        }
                    }

                    if (_waitForPacketsAmount.HasValue)
                    {
                        WaitForPacketList.Add(pack);

                        if (WaitForPacketList.Count != _waitForPacketsAmount)
                        {
                            LastKeepAliveIdentity = packet.KeepAliveId ?? 0;
                            return;
                        }

                        packet = new EntryPointPacket
                        {
                            Header = "EntryPoint",
                            Title = "EntryPoint",
                            KeepAliveId = packet.KeepAliveId,
                            Packet1Id = WaitForPacketList[0].KeepAliveId!.ToString()!,
                            Name = WaitForPacketList[0].Header!,
                            Packet2Id = packet.KeepAliveId!.ToString()!,
                            Password = packet.Header!
                        };

                        _waitForPacketsAmount = null;
                        WaitForPacketList.Clear();
                    }

                    if (packet.Header != "0")
                    {
                        var packetHeader = packet.Header;
                        if (string.IsNullOrWhiteSpace(packetHeader) && (contex != null))
                        {
                            _logger.Warning(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CORRUPT_PACKET),
                                packet);
                            await DisconnectAsync().ConfigureAwait(false);
                            return;
                        }

                        var handler = _packetsHandlers.FirstOrDefault(s =>
                            s.GetType().BaseType?.GenericTypeArguments[0] == packet.GetType());
                        if (handler != null)
                        {
                            if (packet.IsValid)
                            {
                                var attr = _attributeDic[packet.GetType()];
                                if (HasSelectedCharacter && attr.BlockedByTrading && Character.InExchangeOrShop)
                                {
                                    _logger.Warning(
                                        LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PLAYER_IN_SHOP),
                                        packet.Header);
                                    return;
                                }

                                if (!HasSelectedCharacter && !attr.AnonymousAccess)
                                {
                                    _logger.Warning(
                                        LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PACKET_USED_WITHOUT_CHARACTER),
                                        packet.Header);
                                    return;
                                }

                                //check for the correct authority
                                if (IsAuthenticated && attr is CommandPacketHeaderAttribute commandHeader &&
                                    ((byte)commandHeader.Authority > (byte)Account.Authority))
                                {
                                    return;
                                }

                                await handler.ExecuteAsync(packet, this).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            _logger.Warning(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.HANDLER_NOT_FOUND),
                                packet.Header);
                        }
                    }
                }
                else
                {
                    var packetHeader = packet.Header;
                    if (string.IsNullOrWhiteSpace(packetHeader))
                    {
                        await DisconnectAsync().ConfigureAwait(false);
                        return;
                    }

                    var handler = _packetsHandlers.FirstOrDefault(s =>
                        s.GetType().BaseType?.GenericTypeArguments[0] == packet.GetType());
                    if (handler != null)
                    {
                        await handler.ExecuteAsync(packet, this).ConfigureAwait(false);
                    }
                    else
                    {
                        _logger.Warning(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.HANDLER_NOT_FOUND),
                            packetHeader);
                    }
                }
            }));
        }
    }
}