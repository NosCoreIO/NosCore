using NosCore.Domain;
using System;

namespace NosCore.Core.Serializing
{
    public class PacketHeaderAttribute : Attribute
    {
        #region Instantiation

        public PacketHeaderAttribute(string identification)
        {
            Identification = identification;
        }

        public PacketHeaderAttribute(string identification, byte amount)
        {
            Identification = identification;
            Amount = amount;

        }
        #endregion

        #region Properties

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

        #endregion
    }
}