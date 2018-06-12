using NosCore.Core.Serializing;

namespace NosCore.Packets.ServerPackets
{
	[PacketHeader("tit")]
	public class TitPacket : PacketDefinition
	{
		[PacketIndex(0)]
		public string ClassType { get; set; }

		[PacketIndex(1)]
		public string Name { get; set; }
	}
}