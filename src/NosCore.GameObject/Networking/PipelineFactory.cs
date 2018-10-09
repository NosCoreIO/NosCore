using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels.Sockets;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Networking;
using NosCore.Shared;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Networking
{
    public class PipelineFactory
    {
        private readonly ISocketChannel _channel;
        private readonly MessageToMessageDecoder<IByteBuffer> _decoder;
        private readonly MessageToMessageEncoder<string> _encoder;
        private readonly ClientSession _clientSession;
        private readonly GameServerConfiguration _configuration;

        public PipelineFactory(ISocketChannel channel, MessageToMessageDecoder<IByteBuffer> decoder, MessageToMessageEncoder<string> encoder, ClientSession clientSession, GameServerConfiguration configuration)
        {
            _channel = channel;
            _decoder = decoder;
            _encoder = encoder;
            _clientSession = clientSession;
            _configuration = configuration;
        }

        public void CreatePipeline()
        {
            SessionFactory.Instance.Sessions[_channel.Id.AsLongText()] = new RegionTypeMapping(0, _configuration.Language);
            var pipeline = _channel.Pipeline;
            pipeline.AddLast(_decoder);
            _clientSession.RegisterChannel(_channel);
            pipeline.AddLast(_clientSession);
            pipeline.AddLast(_encoder);
        }
    }
}
