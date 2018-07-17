using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations;

namespace NosCore.Packets.ServerPackets
{
	[PacketHeader("cond")]
	public class CondPacket : PacketDefinition
	{
		#region Properties

		[PacketIndex(0)]
		public VisualType VisualType { get; set; }

		[PacketIndex(1)]
		public long VisualId { get; set; }

		[PacketIndex(2)]
		public bool NoAttack { get; set; }

		[PacketIndex(3)]
		public bool NoMove { get; set; }

		[PacketIndex(4)]
		public byte Speed { get; set; }

		#endregion
	}
}