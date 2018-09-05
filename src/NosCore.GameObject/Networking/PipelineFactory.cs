using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels.Sockets;
using NosCore.Core;
using NosCore.Core.Networking;

namespace NosCore.GameObject.Networking
{
    public class PipelineFactory
    {
        private readonly ISocketChannel _channel;
        private readonly MessageToMessageDecoder<IByteBuffer> _decoder;
        private readonly MessageToMessageEncoder<string> _encoder;
        private readonly ClientSession _clientSession;

        public PipelineFactory(ISocketChannel channel, MessageToMessageDecoder<IByteBuffer> decoder, MessageToMessageEncoder<string> encoder, ClientSession clientSession)
        {
            _channel = channel;
            _decoder = decoder;
            _encoder = encoder;
            _clientSession = clientSession;
        }

        public void CreatePipeline()
        {
            SessionFactory.Instance.Sessions[_channel.Id.AsLongText()] = 0;
            var pipeline = _channel.Pipeline;
            pipeline.AddLast(_decoder);
            _clientSession.RegisterChannel(_channel);
            pipeline.AddLast(_clientSession);
            pipeline.AddLast(_encoder);
        }
    }
}
