using OpenNosCore.Core.Serializing;
using OpenNosCore.Domain.Interaction;

namespace OpenNosCore.Packets
{
    [PacketHeader("failc")]
    public class FailcPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public LoginFailType Type { get; set; }

        #endregion
    }
}