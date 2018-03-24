using System;
using System.Linq;
using NosCore.Core.Serializing;
using NosCore.Domain;

namespace NosCore.Core.Handling
{
    public class HandlerMethodReference
    {
        #region Instantiation

        public HandlerMethodReference(Action<object, object> handlerMethod, IPacketHandler parentHandler, Type packetBaseParameterType)
        {
            HandlerMethod = handlerMethod;
            ParentHandler = parentHandler;
            PacketDefinitionParameterType = packetBaseParameterType;
            PacketHeaderAttribute = (PacketHeaderAttribute)PacketDefinitionParameterType.GetCustomAttributes(true).FirstOrDefault(ca => ca.GetType().Equals(typeof(PacketHeaderAttribute)));
            Identification = PacketHeaderAttribute?.Identification;
            Authority = PacketHeaderAttribute?.Authority ?? AuthorityType.User;
        }

        #endregion

        #region Properties
        public Action<object, object> HandlerMethod { get; private set; }

        public PacketHeaderAttribute PacketHeaderAttribute { get; set; }
        public AuthorityType Authority { get; }

        /// <summary>
        ///     Unique identification of the Packet by Header
        /// </summary>
        public string Identification { get; }

        public Type PacketDefinitionParameterType { get; }

        public IPacketHandler ParentHandler { get; }

        #endregion
    }
}