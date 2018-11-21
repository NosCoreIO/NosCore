using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations.Interaction;

namespace NosCore.Packets.ClientPackets
{
    [PacketHeader("req_exc")]
    public class ExchangeRequestPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public RequestExchangeType RequestType { get; set; }

        [PacketIndex(1, IsOptional = true)]
        [Range(0, long.MaxValue)]
        public long? VisualId { get; set; }
    }
}
