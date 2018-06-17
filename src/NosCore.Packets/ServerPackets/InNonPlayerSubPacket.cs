using NosCore.Core.Serializing;

namespace NosCore.Packets.ServerPackets
{
	[PacketHeader("in_non_player_subpacket")]
	public class InNonPlayerSubPacket : PacketDefinition
	{
		[PacketIndex(0)]
		public short Dialog { get; set; }

		[PacketIndex(1)]
		public byte Unknown { get; set; }

		[PacketIndex(2, IsOptional = true, RemoveSeparator = true)]
		public InAliveSubPacket InAliveSubPacket { get; set; }
    }
}