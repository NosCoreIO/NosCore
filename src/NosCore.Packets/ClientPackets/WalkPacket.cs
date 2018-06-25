using JetBrains.Annotations;
using NosCore.Core.Serializing;

namespace NosCore.Packets.ClientPackets
{
	[PacketHeader("walk")]
	public class WalkPacket : PacketDefinition
	{
		#region Properties

		[PacketIndex(0)]
		public short XCoordinate { get; set; }

		[PacketIndex(1)]
		public short YCoordinate { get; set; }

		[PacketIndex(2)]
		[UsedImplicitly]
        public short Unknown { get; set; }

		[PacketIndex(3)]
		public short Speed { get; set; }

		#endregion
	}
}