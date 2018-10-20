//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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
using System.Threading.Tasks;
using DotNetty.Transport.Channels;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Handling;
using NosCore.Core.Networking;
using NosCore.Core.Serializing;
using NosCore.Data;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Services.MapInstanceAccess;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations.Group;
using NosCore.Shared.Enumerations.Interaction;
using NosCore.Shared.Enumerations.Map;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Networking
{
    public class ClientSession : NetworkClient, IClientSession
    {
        private readonly Dictionary<PacketHeaderAttribute, Action<object, object>> _controllerMethods =
            new Dictionary<PacketHeaderAttribute, Action<object, object>>();

        private readonly Dictionary<PacketHeaderAttribute, Tuple<IPacketController, Type>> _headerMethod =
            new Dictionary<PacketHeaderAttribute, Tuple<IPacketController, Type>>();

        private readonly bool _isWorldClient;
        private readonly MapInstanceAccessService _mapInstanceAccessService;

        private Character _character;
        private int? _waitForPacketsAmount;

        public ClientSession(GameServerConfiguration configuration, IEnumerable<IPacketController> packetControllers,
            MapInstanceAccessService mapInstanceAccessService) : this(configuration, packetControllers)
        {
            _mapInstanceAccessService = mapInstanceAccessService;
        }

        public ClientSession(GameServerConfiguration configuration, IEnumerable<IPacketController> packetControllers)
        {
            _isWorldClient = configuration is WorldConfiguration;
            foreach (var controller in packetControllers)
            {
                controller.RegisterSession(this);
                foreach (var methodInfo in controller.GetType().GetMethods().Where(x =>
                    x.GetParameters().FirstOrDefault()?.ParameterType.BaseType == typeof(PacketDefinition)))
                {
                    var type = methodInfo.GetParameters().FirstOrDefault()?.ParameterType;
                    var packetheader = (PacketHeaderAttribute) Array.Find(type?.GetCustomAttributes(true),
                        ca => ca.GetType() == typeof(PacketHeaderAttribute));
                    _headerMethod.Add(packetheader, new Tuple<IPacketController, Type>(controller, type));
                    _controllerMethods.Add(packetheader,
                        DelegateBuilder.BuildDelegate<Action<object, object>>(methodInfo));
                }
            }
        }

        public bool GameStarted { get; set; }

        public int LastKeepAliveIdentity { get; set; }

        public IList<string> WaitForPacketList { get; } = new List<string>();

        public int LastPulse { get; set; }

        public AccountDto Account { get; set; }

        public Character Character
        {
            get
            {
                if (_character == null || !HasSelectedCharacter)
                {
                    // cant access an
                    Logger.Log.Warn(
                        string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.CHARACTER_NOT_INIT)));
                }

                return _character;
            }

            private set => _character = value;
        }

        public bool HasCurrentMapInstance => Character?.MapInstance != null;

        public void InitializeAccount(AccountDto accountDto)
        {
            Account = accountDto;
            IsAuthenticated = true;
            ServerManager.Instance.RegisterSession(this);
        }

        public void SetCharacter(Character character)
        {
            Character = character;
            HasSelectedCharacter = true;
            Character.Session = this;
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            if (!(message is string buff))
            {
                return;
            }

            HandlePackets(buff, context);
        }

        public override void ChannelUnregistered(IChannelHandlerContext context)
        {
            ServerManager.Instance.UnregisterSession(this);
            SessionFactory.Instance.Sessions.TryRemove(context.Channel.Id.AsLongText(), out _);
            Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.CLIENT_DISCONNECTED)));
        }

        public void ChangeMap(short? mapId = null, short? mapX = null, short? mapY = null)
        {
            if (Character == null)
            {
                return;
            }

            if (mapId != null)
            {
                Character.MapInstanceId = _mapInstanceAccessService.GetBaseMapInstanceIdByMapId((short) mapId);
            }

            try
            {
                _mapInstanceAccessService.GetMapInstance(Character.MapInstanceId);
            }
            catch
            {
                return;
            }

            ChangeMapInstance(Character.MapInstanceId, mapX, mapY);
        }

        public void ChangeMapInstance(Guid mapInstanceId, int? mapX = null, int? mapY = null)
        {
            if (Character?.MapInstance == null || Character.IsChangingMapInstance)
            {
                return;
            }

            try
            {
                Character.IsChangingMapInstance = true;
                LeaveMap(this);

                Character.MapInstance.Sessions.TryRemove(SessionId, out _);
                if (Character.MapInstance.Sessions.Count == 0)
                {
                    Character.MapInstance.IsSleeping = true;
                }

                if (Character.IsSitting)
                {
                    Character.IsSitting = false;
                }

                Character.MapInstanceId = mapInstanceId;
                Character.MapInstance = _mapInstanceAccessService.GetMapInstance(mapInstanceId);
                if (Character.MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance)
                {
                    Character.MapId = Character.MapInstance.Map.MapId;
                    if (mapX != null && mapY != null)
                    {
                        Character.MapX = (short) mapX;
                        Character.MapY = (short) mapY;
                    }
                }

                if (mapX != null && mapY != null)
                {
                    Character.PositionX = (short) mapX;
                    Character.PositionY = (short) mapY;
                }

                SendPacket(Character.GenerateCInfo());
                SendPacket(Character.GenerateCMode());
                SendPacket(Character.GenerateLev());
                SendPacket(Character.GenerateStat());
                SendPacket(Character.GenerateAt());
                SendPacket(Character.GenerateCond());
                SendPacket(Character.MapInstance.GenerateCMap());
                SendPackets(Character.MapInstance.GetMapItems());
                if (!Character.InvisibleGm)
                {
                    Character.MapInstance.Broadcast(Character.GenerateIn());
                }

                SendPacket(Character.Group.GeneratePinit());
                SendPackets(Character.Group.GeneratePst());

                if (Character.Group.Type == GroupType.Group && Character.Group.Count > 1)
                {
                    Character.MapInstance.Broadcast(Character.Group.GeneratePidx(Character));
                }

                Parallel.ForEach(
                    Character.MapInstance.Sessions.Values.Where(s => s.Character != null && s != this),
                    s => SendPacket(s.Character.GenerateIn()));

                Character.MapInstance.IsSleeping = false;
                Character.MapInstance.Sessions.TryAdd(SessionId, this);

                Character.IsChangingMapInstance = false;
            }
            catch (Exception)
            {
                Logger.Log.Warn(LogLanguage.Instance.GetMessageFromKey(LanguageKey.ERROR_CHANGE_MAP));
                Character.IsChangingMapInstance = false;
            }
        }

        public void LeaveMap(ClientSession session)
        {
            session.SendPacket(new MapOutPacket());
            session.Character.MapInstance.Broadcast(session, session.Character.GenerateOut(), ReceiverType.AllExceptMe);
        }

        public string GetMessageFromKey(LanguageKey languageKey)
        {
            return Language.Instance.GetMessageFromKey(languageKey, Account.Language);
        }

        private void TriggerHandler(string packetHeader, string packet, bool force)
        {
            var methodReference = _controllerMethods.FirstOrDefault(t => t.Key.Identification == packetHeader);
            if (methodReference.Value != null)
            {
                if (!HasSelectedCharacter && !methodReference.Key.AnonymousAccess)
                {
                    return;
                }

                if (!force && methodReference.Key.Amount > 1 && !_waitForPacketsAmount.HasValue)
                {
                    // we need to wait for more
                    _waitForPacketsAmount = methodReference.Key.Amount;
                    WaitForPacketList.Add(packet != string.Empty ? packet : $"1 {packetHeader} ");
                    return;
                }

                try
                {
                    //check for the correct authority
                    if (IsAuthenticated && (byte) methodReference.Key.Authority > (byte) Account.Authority)
                    {
                        return;
                    }

                    var deserializedPacket = PacketFactory.Deserialize(packet, _headerMethod[methodReference.Key].Item2,
                        IsAuthenticated);

                    if (deserializedPacket != null)
                    {
                        methodReference.Value.Invoke(_headerMethod[methodReference.Key].Item1, deserializedPacket);
                    }
                    else
                    {
                        Logger.Log.Warn(string.Format(
                            LogLanguage.Instance.GetMessageFromKey(LanguageKey.CORRUPT_PACKET), packetHeader, packet));
                    }
                }
                catch (Exception ex)
                {
                    // disconnect if something unexpected happens
                    Logger.Log.Error(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.HANDLER_ERROR),
                        ex));
                    Disconnect();
                }
            }
            else
            {
                Logger.Log.Warn(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.HANDLER_NOT_FOUND),
                    packetHeader));
            }
        }

        private void HandlePackets(string packetConcatenated, IChannelHandlerContext contex)
        {
            //determine first packet
            if (_isWorldClient && SessionFactory.Instance.Sessions[contex.Channel.Id.AsLongText()].SessionId == 0)
            {
                var sessionParts = packetConcatenated.Split(' ');
                if (sessionParts.Length == 0)
                {
                    return;
                }

                if (!int.TryParse(sessionParts[0], out var lastka))
                {
                    Disconnect();
                }

                LastKeepAliveIdentity = lastka;

                // set the SessionId if Session Packet arrives
                if (sessionParts.Length < 2)
                {
                    return;
                }

                if (!int.TryParse(sessionParts[1].Split('\\').FirstOrDefault(), out var sessid))
                {
                    return;
                }

                SessionId = sessid;
                SessionFactory.Instance.Sessions[contex.Channel.Id.AsLongText()].SessionId = SessionId;

                Logger.Log.DebugFormat(LogLanguage.Instance.GetMessageFromKey(LanguageKey.CLIENT_ARRIVED), SessionId);

                if (!_waitForPacketsAmount.HasValue)
                {
                    TriggerHandler("EntryPoint", string.Empty, false);
                }

                return;
            }

            foreach (var packet in packetConcatenated.Split(new[] {(char) 0xFF}, StringSplitOptions.RemoveEmptyEntries))
            {
                var packetstring = packet.Replace('^', ' ');
                var packetsplit = packetstring.Split(' ');

                if (_isWorldClient)
                {
                    // keep alive
                    var nextKeepAliveRaw = packetsplit[0];
                    if (!int.TryParse(nextKeepAliveRaw, out var nextKeepaliveIdentity)
                        && nextKeepaliveIdentity != LastKeepAliveIdentity + 1)
                    {
                        Logger.Log.ErrorFormat(LogLanguage.Instance.GetMessageFromKey(LanguageKey.CORRUPTED_KEEPALIVE),
                            ClientId);
                        Disconnect();
                        return;
                    }

                    if (nextKeepaliveIdentity == 0)
                    {
                        if (LastKeepAliveIdentity == ushort.MaxValue)
                        {
                            LastKeepAliveIdentity = nextKeepaliveIdentity;
                        }
                    }
                    else
                    {
                        LastKeepAliveIdentity = nextKeepaliveIdentity;
                    }

                    if (_waitForPacketsAmount.HasValue)
                    {
                        WaitForPacketList.Add(packetstring);
                        var packetssplit = packetstring.Split(' ');
                        // TODO NEED TO BE REWRITED
                        if (packetssplit.Length > 3 && packetsplit[1] == "DAC")
                        {
                            WaitForPacketList.Add("0 CrossServerAuthenticate");
                        }

                        if (WaitForPacketList.Count != _waitForPacketsAmount)
                        {
                            continue;
                        }

                        _waitForPacketsAmount = null;
                        var queuedPackets = string.Join(" ", WaitForPacketList.ToArray());
                        var header = queuedPackets.Split(' ', '^')[1];
                        TriggerHandler(header, queuedPackets, true);
                        WaitForPacketList.Clear();
                        return;
                    }

                    if (packetsplit.Length <= 1)
                    {
                        continue;
                    }

                    if (packetsplit[1].Length >= 1
                        && (packetsplit[1][0] == '/' || packetsplit[1][0] == ':' || packetsplit[1][0] == ';'))
                    {
                        packetsplit[1] = packetsplit[1][0].ToString();
                        packetstring = packetstring.Insert(packetstring.IndexOf(' ') + 2, " ");
                    }

                    if (packetsplit[1] != "0")
                    {
                        TriggerHandler(packetsplit[1].Replace("#", ""), packetstring, false);
                    }
                }
                else
                {
                    var packetHeader = packetstring.Split(' ')[0];
                    if (string.IsNullOrWhiteSpace(packetHeader))
                    {
                        Disconnect();
                        return;
                    }

                    TriggerHandler(packetHeader.Replace("#", ""), packetstring, false);
                }
            }
        }
    }
}