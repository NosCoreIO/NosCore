using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Map;

namespace NosCore.Packets.ServerPackets
{
	[PacketHeader("say")]
	public class SayPacket : PacketDefinition
	{
		#region Properties

		[PacketIndex(0)]
		public VisualType VisualType { get; set; }

		[PacketIndex(1)]
		public long VisualId { get; set; }

		[PacketIndex(2)]
		public SayColorType Type { get; set; }

		[PacketIndex(3)]
		public string Message { get; set; }

		#endregion
	}
}