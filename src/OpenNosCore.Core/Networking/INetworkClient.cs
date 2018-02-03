using System.Collections.Generic;
using OpenNosCore.Core.Encryption;
using OpenNosCore.Core.Serializing;

namespace OpenNosCore.Core.Networking
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