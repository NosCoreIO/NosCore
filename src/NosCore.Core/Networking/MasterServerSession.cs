using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using Newtonsoft.Json;
using NosCore.Core.Logger;
using NosCore.Domain;

namespace NosCore.Core.Networking
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
            Logger.Logger.Log.Warn(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.UNREGISTRED_FROM_MASTER), _id.ToString()));
        }

        public override Task WriteAsync(IChannelHandlerContext context, object message)
        {
            string textWriter = JsonConvert.SerializeObject(message);
            return base.WriteAsync(context, ByteBufferUtil.EncodeString(context.Allocator, textWriter, Encoding.Default));
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, string msg)
        {
            if (msg == null)
                throw new ArgumentNullException(nameof(msg));

            Channel msgChannel;
            try
            {
                msgChannel = JsonConvert.DeserializeObject<Channel>(msg);
            }
            catch (Exception ex)
            {
                Logger.Logger.Log.Error(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.UNRECOGNIZED_MASTER_PACKET), ex));
                return;
            }

            if (!IsAuthenticated)
            {
                if (msgChannel.Password == Password)
                {
                    IsAuthenticated = true;
                    Logger.Logger.Log.Debug(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.AUTHENTICATED_SUCCESS), _id.ToString()));

                    if (MasterClientListSingleton.Instance.WorldServers == null)
                    {
                        MasterClientListSingleton.Instance.WorldServers = new List<WorldServerInfo>();
                    }
                    try
                    {
                        _id = MasterClientListSingleton.Instance.WorldServers.Select(s => s.Id).Max() + 1;
                    }
                    catch
                    {
                        _id = 0;
                    }
                    ServerType servtype = (ServerType)System.Enum.Parse(typeof(ServerType), msgChannel.ClientType.ToString());
                    if (servtype == ServerType.WorldServer)
                    {
                        WorldServerInfo serv = new WorldServerInfo
                        {
                            Name = msgChannel.ClientName,
                            Host = msgChannel.Host,
                            Port = msgChannel.Port,
                            Id = _id,
                            ConnectedAccountsLimit = msgChannel.ConnectedAccountsLimit,
                            WebApi = msgChannel.WebApi
                        };

                        MasterClientListSingleton.Instance.WorldServers.Add(serv);
                        WriteAsync(ctx, msgChannel);
                    }
                    ctx.Flush();
                }
                else
                {
                    ctx.CloseAsync();
                    Logger.Logger.Log.Error(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.AUTHENTICATED_ERROR)));
                }
            }
        }

        public override void ChannelRegistered(IChannelHandlerContext context)
        {
            Logger.Logger.Log.Debug(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.REGISTRED_FROM_MASTER)));
        }
    }
}
