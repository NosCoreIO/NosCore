using NosCore.Core.Serializing;

namespace NosCore.Packets.ServerPackets
{
	[PacketHeader("out")]
	public class OutPacket : PacketDefinition
	{
        #region Properties
		[PacketIndex(0)]
		public byte VisualType { get; set; }

		[PacketIndex(1)]
		public long VisualId { get; set; }
        #endregion
    }
}