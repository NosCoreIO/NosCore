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
using System.Collections.Concurrent;
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
using Serilog;

namespace NosCore.GameObject.Networking.ClientSession
{
    public class ClientSession : NetworkClient, IClientSession
    {
        private readonly Dictionary<Type, PacketHeaderAttribute> _attributeDic =
            new Dictionary<Type, PacketHeaderAttribute>();

        private readonly IExchangeProvider _exchangeProvider;
        private readonly IFriendHttpClient _friendHttpClient;

        private readonly bool _isWorldClient;
        private readonly ILogger _logger;
        private readonly IMapInstanceProvider _mapInstanceProvider;
        private readonly IMinilandProvider _minilandProvider;
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
            if (configuration is WorldConfiguration worldConfiguration)
            {
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
        }

        public WorldConfiguration WorldConfiguration { get; }

        public bool GameStarted { get; set; }

        public int LastKeepAliveIdentity { get; set; }

        public IList<IPacket> WaitForPacketList { get; } = new List<IPacket>();

        public int LastPulse { get; set; }

        public AccountDto Account { get; set; }

        public Character Character
        {
            get
            {
                if ((_character != null) && !HasSelectedCharacter)
                {
                    // cant access an
                    _logger.Warning(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CHARACTER_NOT_INIT));
                    throw new NullReferenceException();
                }

                return _character!;
            }

            private set => _character = value;
        }

        public bool HasCurrentMapInstance => Character?.MapInstance != null;

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
            if (character != null)
            {
                Character.Session = this;
                _minilandProvider?.Initialize(character);
            }
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            if (!(message is IEnumerable<IPacket> buff))
            {
                return;
            }

            //https://github.com/Azure/DotNetty/issues/265
            var runner = Task.Run<Task>(async () =>
                await HandlePackets(buff, context)
            ).Result;
        }

        public override async void ChannelUnregistered(IChannelHandlerContext context)
        {
            try
            {
                if (!(_character?.IsDisconnecting ?? false))
                {
                    if (_character != null)
                    {
                        Character.IsDisconnecting = true;
                        if (Character.Hp < 1)
                        {
                            Character.Hp = 1;
                        }

                        await Character.SendFinfo(_friendHttpClient, _packetHttpClient, _packetSerializer, false);

                        var targetId = _exchangeProvider.GetTargetId(Character.VisualId);
                        if (targetId.HasValue)
                        {
                            var closeExchange =
                                _exchangeProvider.CloseExchange(Character.VisualId, ExchangeResultType.Failure);
                            if (Broadcaster.Instance.GetCharacter(s => s.VisualId == targetId) is Character target)
                            {
                                await target.SendPacket(closeExchange);
                            }
                        }

                        await Character.LeaveGroup();
                        Character.MapInstance?.SendPacket(Character.GenerateOut());
                        Character.Save();

                        _minilandProvider.DeleteMiniland(Character.CharacterId);
                    }

                    Broadcaster.Instance.UnregisterSession(this);
                    _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CLIENT_DISCONNECTED));
                }
            }
            catch
            {
                _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CLIENT_DISCONNECTED));
            }
        }

        public Task ChangeMap()
        {
            return ChangeMap(null, null, null);
        }

        public async Task ChangeMap(short? mapId, short? mapX, short? mapY)
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

            await ChangeMapInstance(Character.MapInstanceId, mapX, mapY);
        }

        public async Task ChangeMapInstance(Guid mapInstanceId)
        {
            await ChangeMapInstance(mapInstanceId, null, null);
        }

        public async Task ChangeMapInstance(Guid mapInstanceId, int? mapX, int? mapY)
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
                await LeaveMap();
                if (Character.MapInstance.Sessions.Count == 0)
                {
                    Character.MapInstance.IsSleeping = true;
                }

                if (Character.IsSitting)
                {
                    Character.IsSitting = false;
                }

                Character.MapInstanceId = mapInstanceId;
                Character.MapInstance = _mapInstanceProvider.GetMapInstance(mapInstanceId);

                if (Character.MapInstance!.MapInstanceType == MapInstanceType.BaseMapInstance)
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

                await SendPacket(Character.GenerateCInfo());
                await SendPacket(Character.GenerateCMode());
                await SendPacket(Character.GenerateEq());
                await SendPacket(Character.GenerateEquipment());
                await SendPacket(Character.GenerateLev());
                await SendPacket(Character.GenerateStat());
                await SendPacket(Character.GenerateAt());
                await SendPacket(Character.GenerateCond());
                await SendPacket(Character.MapInstance.GenerateCMap());
                await SendPacket(Character.GeneratePairy(
                    Character.InventoryService!.LoadBySlotAndType((byte)EquipmentType.Fairy,
                        NoscorePocketType.Wear)?.ItemInstance as WearableInstance));
                await SendPackets(Character.MapInstance.GetMapItems());
                await SendPackets(Character.MapInstance.MapDesignObjects.Values.Select(mp => mp.GenerateEffect()));

                var minilandPortals = _minilandProvider
                    .GetMinilandPortals(Character.CharacterId)
                    .Where(s => s.SourceMapInstanceId == mapInstanceId)
                    .ToList();

                if (minilandPortals.Count > 0)
                {
                    await SendPackets(minilandPortals.Select(s => s.GenerateGp()));
                }

                await SendPacket(Character.Group!.GeneratePinit());
                if (!Character.Group.IsEmpty)
                {
                    await SendPackets(Character.Group.GeneratePst());
                }

                if ((Character.Group.Type == GroupType.Group) && (Character.Group.Count > 1))
                {
                    await Character.MapInstance.SendPacket(Character.Group.GeneratePidx(Character));
                }

                var mapSessions = Broadcaster.Instance.GetCharacters(s =>
                    (s != Character) && (s.MapInstance!.MapInstanceId == Character.MapInstanceId));

                Parallel.ForEach(mapSessions, s =>
                {
                    SendPacket(s.GenerateIn(s.Authority == AuthorityType.Moderator
                        ? $"[{Language.Instance.GetMessageFromKey(LanguageKey.SUPPORT, s.AccountLanguage)}]"
                        : string.Empty));
                    if (s.Shop != null)
                    {
                        SendPacket(s.GeneratePFlag());
                        SendPacket(s.GenerateShop());
                    }
                });

                await Character.MapInstance.SendPacket(Character.GenerateTitInfo());
                Character.MapInstance.IsSleeping = false;
                if (Channel?.Id != null)
                {
                    if (!Character.Invisible)
                    {
                        await Character.MapInstance.SendPacket(Character.GenerateIn(Character.Authority == AuthorityType.Moderator
                                ? $"[{Character.Session.GetMessageFromKey(LanguageKey.SUPPORT)}]" : string.Empty),
                            new EveryoneBut(Character.Channel!.Id));
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

        private async Task LeaveMap()
        {
            await SendPacket(new MapOutPacket());
            await Character.MapInstance!.SendPacket(Character.GenerateOut(),
                new EveryoneBut(Channel!.Id));
        }

        public string GetMessageFromKey(LanguageKey languageKey)
        {
            return Language.Instance.GetMessageFromKey(languageKey, Account.Language);
        }

        public async Task HandlePackets(IEnumerable<IPacket> packetConcatenated, IChannelHandlerContext? contex = null)
        {
            foreach (var pack in packetConcatenated)
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
                            await Disconnect();
                            return;
                        }

                        if (!_waitForPacketsAmount.HasValue && (LastKeepAliveIdentity == 0))
                        {
                            SessionId = SessionFactory.Instance.Sessions[contex.Channel.Id.AsLongText()].SessionId;
                            _logger.Debug(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CLIENT_ARRIVED),
                                SessionId);
                            _waitForPacketsAmount = 2;
                            continue;
                        }

                        LastKeepAliveIdentity = (ushort)packet.KeepAliveId!;

                        if (packet.KeepAliveId == null)
                        {
                            await Disconnect();
                        }
                    }

                    if (_waitForPacketsAmount.HasValue)
                    {
                        WaitForPacketList.Add(pack);

                        if (WaitForPacketList.Count != _waitForPacketsAmount)
                        {
                            LastKeepAliveIdentity = packet.KeepAliveId ?? 0;
                            continue;
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
                            await Disconnect();
                            return;
                        }

                        var handler = _packetsHandlers.FirstOrDefault(s =>
                            s.GetType().BaseType?.GenericTypeArguments[0] == packet.GetType());
                        if (handler != null)
                        {
                            if (packet.IsValid)
                            {
                                var attr = _attributeDic[packet.GetType()];
                                if (!HasSelectedCharacter && !attr.AnonymousAccess)
                                {
                                    _logger.Warning(
                                        LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PACKET_USED_WITHOUT_CHARACTER),
                                        packet.Header);
                                    continue;
                                }

                                //check for the correct authority
                                if (IsAuthenticated && attr is CommandPacketHeaderAttribute commandHeader &&
                                    ((byte)commandHeader.Authority > (byte)Account.Authority))
                                {
                                    continue;
                                }

                                await handler.Execute(packet, this);
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
                        await Disconnect();
                        return;
                    }

                    var handler = _packetsHandlers.FirstOrDefault(s =>
                        s.GetType().BaseType?.GenericTypeArguments[0] == packet.GetType());
                    if (handler != null)
                    {
                        await handler.Execute(packet, this);
                    }
                    else
                    {
                        _logger.Warning(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.HANDLER_NOT_FOUND),
                            packetHeader);
                    }
                }
            }
        }
    }
}