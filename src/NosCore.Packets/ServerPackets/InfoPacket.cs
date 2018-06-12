using NosCore.Core.Serializing;

namespace NosCore.Packets.ServerPackets
{
	[PacketHeader("info")]
	public class InfoPacket : PacketDefinition
	{
		#region Properties

		[PacketIndex(0, serializeToEnd: true)]
		public string Message { get; set; }

		#endregion
	}
}