using NosCore.Core.Serializing;
using NosCore.Domain.Account;
using System;
using System.Linq;

namespace NosCore.Core.Handling
{
    public class HandlerMethodReference
    {

        public HandlerMethodReference(Type packetBaseParameterType)
        {
            PacketDefinitionParameterType = packetBaseParameterType;
            PacketHeaderAttribute = (PacketHeaderAttribute)PacketDefinitionParameterType.GetCustomAttributes(true).FirstOrDefault(ca => ca.GetType().Equals(typeof(PacketHeaderAttribute)));
            Identification = PacketHeaderAttribute?.Identification;
            Authority = PacketHeaderAttribute?.Authority ?? AuthorityType.User;
        }

        public PacketHeaderAttribute PacketHeaderAttribute { get; set; }

        public AuthorityType Authority { get; }

        public string Identification { get; }

        public Type PacketDefinitionParameterType { get; }
    }
}