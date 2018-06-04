using DotNetty.Transport.Channels;
using NosCore.Core;
using NosCore.Core.Networking;
using NosCore.Core.Serializing;
using NosCore.Data;
using NosCore.Shared.Logger;
using NosCore.Shared.Map;
using NosCore.GameObject.ComponentEntities.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NosCore.GameObject.Networking
{
    public class ClientSession : NetworkClient, IClientSession
    {
        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            if (!(message is string buff))
            {
                return;
            }
            HandlePackets(buff, context);
        }

        public bool HealthStop;
        private Character _character;
        private readonly Random _random;
        private readonly bool _isWorldClient;
        private int? _waitForPacketsAmount;

        // private byte countPacketReceived;
        private readonly long lastPacketReceive;
        public ClientSession() : base(null) { }

        public ClientSession(IChannel channel, bool isWorldClient) : base(channel)
        {
            // set last received
            lastPacketReceive = DateTime.Now.Ticks;
            _random = new Random((int)ClientId);
            _isWorldClient = isWorldClient;
            foreach (var controller in PacketControllerFactory.GenerateControllers())
            {
                controller.RegisterSession(this);
                foreach (MethodInfo methodInfo in controller.GetType().GetMethods().Where(x => x.GetParameters().FirstOrDefault()?.ParameterType.BaseType == typeof(PacketDefinition)))
                {
                    var type = methodInfo.GetParameters().FirstOrDefault()?.ParameterType;
                    PacketHeaderAttribute packetheader = (PacketHeaderAttribute)Array.Find(type.GetCustomAttributes(true), ca => ca.GetType().Equals(typeof(PacketHeaderAttribute)));
                    HeaderMethod.Add(packetheader, new Tuple<IPacketController, Type>(controller, type));
                    ControllerMethods.Add(packetheader, DelegateBuilder.BuildDelegate<Action<object, object>>(methodInfo));
                }
            }
        }

        private readonly Dictionary<PacketHeaderAttribute, Tuple<IPacketController, Type>> HeaderMethod = new Dictionary<PacketHeaderAttribute, Tuple<IPacketController, Type>>();
        private readonly Dictionary<PacketHeaderAttribute, Action<object, object>> ControllerMethods = new Dictionary<PacketHeaderAttribute, Action<object, object>>();

        public override void ChannelUnregistered(IChannelHandlerContext context)
        {
            SessionFactory.Instance.Sessions.TryRemove(context.Channel.Id.AsLongText(), out int i);
            ServerManager.Instance.UnregisterSession(this);
            Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CLIENT_DISCONNECTED)));
        }

        public AccountDTO Account { get; set; }

        public Character Character
        {
            get
            {
                if (_character == null || !HasSelectedCharacter)
                {
                    // cant access an
                    Logger.Log.Warn(string.Format(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CHARACTER_NOT_INIT)));
                }

                return _character;
            }

            private set
            {
                _character = value;
            }
        }

        public MapInstance CurrentMapInstance { get; set; }
        public bool HasCurrentMapInstance => CurrentMapInstance != null;
        public bool IsOnMap => CurrentMapInstance != null;

        public int LastKeepAliveIdentity { get; set; }

        public IList<string> WaitForPacketList { get; } = new List<string>();

        public void InitializeAccount(AccountDTO accountDTO)
        {
            Account = accountDTO;
            IsAuthenticated = true;
            ServerManager.Instance.RegisterSession(this);
        }

        public void ChangeMap(short? mapId = null, short? mapX = null, short? mapY = null)
        {
            if (Character == null)
            {
                return;
            }
            if (mapId != null)
            {
                Character.MapInstanceId = ServerManager.Instance.GetBaseMapInstanceIdByMapId((short)mapId);
            }
            try
            {
                ServerManager.Instance.GetMapInstance(Character.MapInstanceId);
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
                Character.MapInstance.Sessions.TryRemove(SessionId, out ClientSession sess);
                if (Character.IsSitting)
                {
                    Character.IsSitting = false;
                }
                Character.MapInstanceId = mapInstanceId;
                if (Character.MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance)
                {
                    Character.MapId = Character.MapInstance.Map.MapId;
                    if (mapX != null && mapY != null)
                    {
                        Character.MapX = (short)mapX;
                        Character.MapY = (short)mapY;
                    }
                }
                if (mapX != null && mapY != null)
                {
                    Character.PositionX = (short)mapX;
                    Character.PositionY = (short)mapY;
                }

                SendPacket(Character.GenerateCInfo());
                SendPacket(Character.GenerateCMode());
                SendPacket(Character.GenerateAt());
                SendPacket(Character.GenerateCond());
                SendPacket(Character.MapInstance.GenerateCMap());
                SendPacket(Character.GenerateIn());
                Character.MapInstance.Sessions.TryAdd(SessionId, this);
                Character.IsChangingMapInstance = false;
            }
            catch (Exception)
            {
                Logger.Log.Warn(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.ERROR_CHANGE_MAP));
                Character.IsChangingMapInstance = false;
            }
        }

        public void SetCharacter(Character character)
        {
            Character = character;
            HasSelectedCharacter = true;
            Character.Session = this;
        }

        private void TriggerHandler(string packetHeader, string packet, bool force)
        {
            var methodReference = ControllerMethods.FirstOrDefault(t => t.Key.Identification == packetHeader);
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
                    if (IsAuthenticated && (byte)methodReference.Key.Authority > (byte)Account.Authority)
                    {
                        return;
                    }
                    PacketDefinition deserializedPacket = PacketFactory.Deserialize(packet, HeaderMethod[methodReference.Key].Item2, IsAuthenticated);

                    if (deserializedPacket != null)
                    {
                        methodReference.Value.Invoke(HeaderMethod[methodReference.Key].Item1, deserializedPacket);
                    }
                    else
                    {
                        Logger.Log.Warn(string.Format(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CORRUPT_PACKET), packetHeader, packet));
                    }
                }
                catch (Exception ex)
                {
                    // disconnect if something unexpected happens
                    Logger.Log.Error(string.Format(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.HANDLER_ERROR), ex));
                    Disconnect();
                }
            }
            else
            {
                Logger.Log.Warn(string.Format(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.HANDLER_NOT_FOUND), packetHeader));
            }
        }

        private void HandlePackets(string packetConcatenated, IChannelHandlerContext contex)
        {
            //determine first packet
            if (_isWorldClient && SessionFactory.Instance.Sessions[contex.Channel.Id.AsLongText()] == 0)
            {
                string[] SessionParts = packetConcatenated.Split(' ');
                if (SessionParts.Length == 0)
                {
                    return;
                }
                if (!int.TryParse(SessionParts[0], out int lastka))
                {
                    Disconnect();
                }
                LastKeepAliveIdentity = lastka;

                // set the SessionId if Session Packet arrives
                if (SessionParts.Length < 2)
                {
                    return;
                }
                if (!int.TryParse(SessionParts[1].Split('\\').FirstOrDefault(), out int sessid))
                {
                    return;
                }
                SessionId = sessid;
                SessionFactory.Instance.Sessions[contex.Channel.Id.AsLongText()] = SessionId;

                Logger.Log.DebugFormat(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CLIENT_ARRIVED), SessionId);

                if (!_waitForPacketsAmount.HasValue)
                {
                    TriggerHandler("EntryPoint", string.Empty, false);
                }
                return;
            }

            foreach (string packet in packetConcatenated.Split(new[] { (char)0xFF }, StringSplitOptions.RemoveEmptyEntries))
            {
                string packetstring = packet.Replace('^', ' ');
                string[] packetsplit = packetstring.Split(' ');

                if (_isWorldClient)
                {
                    // keep alive
                    string nextKeepAliveRaw = packetsplit[0];
                    if (!int.TryParse(nextKeepAliveRaw, out int nextKeepaliveIdentity) && nextKeepaliveIdentity != (LastKeepAliveIdentity + 1))
                    {
                        Logger.Log.ErrorFormat(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CORRUPTED_KEEPALIVE), ClientId);
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
                        string[] packetssplit = packetstring.Split(' ');
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
                        string queuedPackets = string.Join(" ", WaitForPacketList.ToArray());
                        string header = queuedPackets.Split(' ', '^')[1];
                        TriggerHandler(header, queuedPackets, true);
                        WaitForPacketList.Clear();
                        return;
                    }
                    if (packetsplit.Length <= 1)
                    {
                        continue;
                    }
                    if (packetsplit[1].Length >= 1 && (packetsplit[1][0] == '/' || packetsplit[1][0] == ':' || packetsplit[1][0] == ';'))
                    {
                        packetsplit[1] = packetsplit[1][0].ToString();
                        packetstring = packet.Insert(packet.IndexOf(' ') + 2, " ");
                    }
                    if (packetsplit[1] != "0")
                    {
                        TriggerHandler(packetsplit[1].Replace("#", ""), packetstring, false);
                    }
                }
                else
                {
                    string packetHeader = packetstring.Split(' ')[0];
                    if (string.IsNullOrWhiteSpace(packetHeader))
                    {
                        Disconnect();
                        return;
                    }
                    // simple messaging
                    if (packetHeader[0] == '/' || packetHeader[0] == ':' || packetHeader[0] == ';')
                    {
                        packetHeader = packetHeader[0].ToString();
                        packetstring = packet.Insert(packet.IndexOf(' ') + 2, " ");
                    }

                    TriggerHandler(packetHeader.Replace("#", ""), packetstring, false);
                }
            }
        }
    }
}
