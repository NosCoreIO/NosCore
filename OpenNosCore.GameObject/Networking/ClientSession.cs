using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using OpenNosCore.Core;
using OpenNosCore.Core.Encryption;
using OpenNosCore.Core.Handling;
using OpenNosCore.Core.Logger;
using OpenNosCore.Core.Networking;
using OpenNosCore.Core.Serializing;
using OpenNosCore.Data;
using OpenNosCore.Domain.Map;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace OpenNosCore.GameObject
{

    public class ClientSession : NetworkClient
    {
        public override void ChannelRead(IChannelHandlerContext contex, object msg)
        {
            if (!(msg is IByteBuffer buff))
            {
                return;
            }
            byte[] data = new byte[buff.ReadableBytes];
            buff.GetBytes(buff.ReaderIndex, data);
            handlePackets(data);
        }

        public bool HealthStop = false;

        private static IEncryptor _encryptor;
        private Character _character;
        private Random _random;
   

        private readonly IList<string> _waitForPacketList = new List<string>();

        private IDictionary<string, HandlerMethodReference> _handlerMethods;
        private int? _waitForPacketsAmount;
        bool firstPacketReceived;

        // private byte countPacketReceived;
        private long lastPacketReceive;
        
        public ClientSession(IEncryptor encryptor, IChannel channel, IEnumerable<IPacketHandler> packetList) : base(encryptor,channel)
        {
            // set last received
            lastPacketReceive = DateTime.Now.Ticks;

            // lag mode
            _random = new Random((int)ClientId);

            // absolutely new instantiated Client has no SessionId
            SessionId = 0;

            _encryptor = encryptor;

            // dynamically create packethandler references
            GenerateHandlerReferences(packetList);
        }
        

        public AccountDTO Account { get; private set; }

        public Character Character
        {
            get
            {
                if (_character == null || !HasSelectedCharacter)
                {
                    // cant access an
                    Logger.Log.Warn("Uninitialized Character cannot be accessed.");
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
        

        public void Initialize(EncryptionBase encryptor, IEnumerable<IPacketHandler> packetHandler)
        {
            _encryptor = encryptor;

            // dynamically create packethandler references
            GenerateHandlerReferences(packetHandler);
        }

        public void InitializeAccount(AccountDTO account)
        {
            Account = account;
            IsAuthenticated = true;
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
            if (Character == null || Character.IsChangingMapInstance)
            {
                return;
            }
            try
            {
                Character.IsChangingMapInstance = true;
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
                Character.IsChangingMapInstance = false;
            }
            catch (Exception)
            {
                Logger.Log.Warn(LogLanguage.Instance.GetMessageFromKey("ERROR_CHANGE_MAP"));
                Character.IsChangingMapInstance = false;
            }
        }

        public void SetCharacter(Character character)
        {
            Character = character;
            HasSelectedCharacter = true;

            // register for servermanager
            ServerManager.Instance.RegisterSession(this);
            Character.SetSession(this);
        }


        private void GenerateHandlerReferences(IEnumerable<IPacketHandler> packetDictionary)
        {
            // iterate thru each type in the given assembly
            foreach (IPacketHandler handlerType in packetDictionary)
            {
                IPacketHandler handler = (IPacketHandler)Activator.CreateInstance(handlerType.GetType(), this);

                // include PacketDefinition
                foreach (MethodInfo methodInfo in handlerType.GetType().GetMethods().Where(x => x.GetParameters().FirstOrDefault()?.ParameterType.BaseType == typeof(PacketDefinition)))
                {
                    HandlerMethodReference methodReference = new HandlerMethodReference(DelegateBuilder.BuildDelegate<Action<object, object>>(methodInfo), handler, methodInfo.GetParameters().FirstOrDefault()?.ParameterType);
                    HandlerMethods.Add(methodReference.Identification, methodReference);
                }
            }
        }

        public IDictionary<string, HandlerMethodReference> HandlerMethods
        {
            get { return _handlerMethods ?? (_handlerMethods = new Dictionary<string, HandlerMethodReference>()); }

            set { _handlerMethods = value; }
        }

        private void TriggerHandler(string packetHeader, string packet, bool force)
        {
            HandlerMethodReference methodReference = HandlerMethods.ContainsKey(packetHeader) ? HandlerMethods[packetHeader] : null;
            if (methodReference != null)
            {
                if (methodReference.PacketHeaderAttribute != null && !force && methodReference.PacketHeaderAttribute.Amount > 1 && !_waitForPacketsAmount.HasValue)
                {
                    // we need to wait for more
                    _waitForPacketsAmount = methodReference.PacketHeaderAttribute.Amount;
                    _waitForPacketList.Add(packet != string.Empty ? packet : $"1 {packetHeader} ");
                    return;
                }
                try
                {
                    if (!HasSelectedCharacter && !(methodReference.ParentHandler is ICharacterScreenPacketHandler) &&
                        !(methodReference.ParentHandler is ILoginPacketHandler))
                    {
                        return;
                    }
                    // call actual handler method
                    if (methodReference.PacketDefinitionParameterType != null)
                    {
                        //check for the correct authority
                        if (IsAuthenticated && (byte)methodReference.Authority > (byte)Account.Authority)
                        {
                            return;
                        }
                        object deserializedPacket = PacketFactory.Deserialize(packet, methodReference.PacketDefinitionParameterType, IsAuthenticated);

                        if (deserializedPacket != null)
                        {
                            methodReference.HandlerMethod(methodReference.ParentHandler, deserializedPacket);
                        }
                        else
                        {
                            Logger.Log.Warn(string.Format(Language.Instance.GetMessageFromKey("CORRUPT_PACKET"), packetHeader, packet));
                        }
                    }
                    else
                    {
                        methodReference.HandlerMethod(methodReference.ParentHandler, packet);
                    }
                }
                catch (DivideByZeroException ex)
                {
                    // disconnect if something unexpected happens
                    Logger.Log.Error(string.Format(Language.Instance.GetMessageFromKey("HANDLER_ERROR"), SessionId, ex));
                    Disconnect();
                }
            }
            else
            {
                Logger.Log.Warn(string.Format(Language.Instance.GetMessageFromKey("HANDLER_NOT_FOUND"), packetHeader));
            }
        }

        private void handlePackets(byte[] packetData)
        {
            string packetConcatenated = _encryptor.Decrypt(packetData, SessionId);

            // determine first packet
            if (_encryptor.HasCustomParameter && !firstPacketReceived)
            {
                firstPacketReceived = true;
                string SessionPacket = _encryptor.DecryptCustomParameter(packetData);

                string[] SessionParts = SessionPacket.Split(' ');
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
                Logger.Log.DebugFormat(LogLanguage.Instance.GetMessageFromKey("CLIENT_ARRIVED"), SessionId);

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

                if (_encryptor.HasCustomParameter)
                {
                    // keep alive
                    string nextKeepAliveRaw = packetsplit[0];
                    if (!int.TryParse(nextKeepAliveRaw, out int nextKeepaliveIdentity) && nextKeepaliveIdentity != (LastKeepAliveIdentity + 1))
                    {
                        Logger.Log.ErrorFormat(LogLanguage.Instance.GetMessageFromKey("CORRUPTED_KEEPALIVE"), ClientId);
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
                        _waitForPacketList.Add(packetstring);
                        string[] packetssplit = packetstring.Split(' ');
                        // TODO NEED TO BE REWRITED
                        if (packetssplit.Length > 3 && packetsplit[1] == "DAC")
                        {
                            _waitForPacketList.Add("0 CrossServerAuthenticate");
                        }
                        if (_waitForPacketList.Count != _waitForPacketsAmount)
                        {
                            continue;
                        }
                        _waitForPacketsAmount = null;
                        string queuedPackets = string.Join(" ", _waitForPacketList.ToArray());
                        string header = queuedPackets.Split(' ', '^')[1];
                        TriggerHandler(header, queuedPackets, true);
                        _waitForPacketList.Clear();
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
