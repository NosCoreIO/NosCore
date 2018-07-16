using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations.Map;

namespace NosCore.Packets.ServerPackets
{
	[PacketHeader("out")]
	public class OutPacket : PacketDefinition
	{
        #region Properties
		[PacketIndex(0)]
		public VisualType VisualType { get; set; }

		[PacketIndex(1)]
		public long VisualId { get; set; }
        #endregion
    }
}