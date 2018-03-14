using System.Collections.Generic;
using NosCore.Core.Encryption;
using NosCore.Core.Serializing;

namespace NosCore.Core.Networking
{
    public interface INetworkClient
    {
        #region Properties

        long ClientId { get; set; }

        #endregion

        #region Methods

        void Disconnect();

        void SendPacket(PacketDefinition packet);

        void SendPackets(IEnumerable<PacketDefinition> packets);

        #endregion
    }
}