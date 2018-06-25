using System.Collections.Generic;
using NosCore.Core.Serializing;

namespace NosCore.Packets.ServerPackets
{
	[PacketHeader("NsTeST")]
	public class NsTestPacket : PacketDefinition
	{
		#region Properties

		[PacketIndex(0)]
		public string AccountName { get; set; }

		[PacketIndex(1)]
		public int SessionId { get; set; }

		[PacketIndex(2)]
		public List<NsTeStSubPacket> SubPacket { get; set; }

		#endregion
	}
}