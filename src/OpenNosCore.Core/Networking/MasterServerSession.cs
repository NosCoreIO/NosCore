using System;
using System.Collections.Generic;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using System.Reflection;
using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;
using System.Linq;
using OpenNosCore.Core.Logger;
using OpenNosCore.Master.Objects;
using OpenNosCore.Core;

namespace OpenNosCore.Networking
{
    public class MasterServerSession : SimpleChannelInboundHandler<string>
    {
        protected readonly string Password;
        protected bool IsAuthenticated;
        private int _id;

        public MasterServerSession(string password)
        {
            Password = password;
        }

        public override void ChannelUnregistered(IChannelHandlerContext context)
        {
            MasterClientListSingleton.Instance.WorldServers?.RemoveAll(s => s.Id == _id);
            Logger.Log.Warn(string.Format(LogLanguage.Instance.GetMessageFromKey(string.Format("UNREGISTRED_FROM_MASTER", _id.ToString()))));
        }

        public override Task WriteAsync(IChannelHandlerContext context, object message)
        {
            string textWriter = JsonConvert.SerializeObject(message);
            return base.WriteAsync(context, ByteBufferUtil.EncodeString(context.Allocator, textWriter, Encoding.Default));
        }

        protected override void ChannelRead0(IChannelHandlerContext contex, string toDeserialize)
        {
            Channel msg;
            try
            {
                msg = JsonConvert.DeserializeObject<Channel>(toDeserialize);
            }
            catch (Exception ex)
            {
                Logger.Log.Error(string.Format(LogLanguage.Instance.GetMessageFromKey("UNRECOGNIZED_MASTER_PACKET"), ex));
                return;
            }

            if (!IsAuthenticated)
            {
                if (msg.Password == Password)
                {
                    IsAuthenticated = true;
                    Logger.Log.Debug(string.Format(LogLanguage.Instance.GetMessageFromKey(string.Format("AUTHENTICATED_SUCCESS", _id.ToString()))));

                    if (MasterClientListSingleton.Instance.WorldServers == null)
                    {
                        MasterClientListSingleton.Instance.WorldServers = new List<WorldServer>();
                    }
                    try
                    {
                        _id = MasterClientListSingleton.Instance.WorldServers.Select(s => s.Id).Max() + 1;
                    }
                    catch
                    {
                        _id = 0;
                    }
                    ServerType servtype = (ServerType)System.Enum.Parse(typeof(ServerType), msg.ClientType.ToString());
                    if (servtype == ServerType.WorldServer)
                    {
                        WorldServer serv = new WorldServer
                        {
                            Name = msg.ClientName,
                            Host = msg.Host,
                            Port = msg.Port,
                            Id = _id,
                            ConnectedAccountsLimit = msg.ConnectedAccountsLimit,
                            WebApi = msg.WebApi
                        };

                        MasterClientListSingleton.Instance.WorldServers.Add(serv);
                        WriteAsync(contex, msg);

                    }
                    contex.Flush();
                }
                else
                {
                    contex.CloseAsync();
                    Logger.Log.Error(string.Format(LogLanguage.Instance.GetMessageFromKey("AUTHENTICATED_ERROR")));
                }
            }
        }

        public override void ChannelRegistered(IChannelHandlerContext context)
        {
            Logger.Log.Debug(string.Format(LogLanguage.Instance.GetMessageFromKey("REGISTRED_FROM_MASTER")));
        }

        private Type GetType(string name)
        {
            Type type = null;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(name);
                if (type != null)
                {
                    break;
                }
            }
            return type;
        }
    }
}
