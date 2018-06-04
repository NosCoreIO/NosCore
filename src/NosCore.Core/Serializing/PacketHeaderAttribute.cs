using NosCore.Shared;
using System;
using NosCore.Shared.Account;

namespace NosCore.Core.Serializing
{
    [AttributeUsageAttribute(AttributeTargets.All, AllowMultiple = false)]
    public class PacketHeaderAttribute : Attribute
    {
        public PacketHeaderAttribute(string identification)
        {
            Identification = identification;
        }

        public PacketHeaderAttribute(string identification, byte amount)
        {
            Identification = identification;
            Amount = amount;
        }

        /// <summary>
        ///     Permission to handle the packet
        /// </summary>
        public AuthorityType Authority { get; set; }

        /// <summary>
        ///     Unique identification of the Packet
        /// </summary>
        public string Identification { get; set; }

        /// <summary>
        ///     Amount of tcp message to create the Packet
        /// </summary>
        public byte Amount { get; set; }
        public bool AnonymousAccess { get; set; }
    }
}