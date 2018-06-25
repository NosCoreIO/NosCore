using System;

namespace NosCore.Core.Serializing
{
	[AttributeUsage(AttributeTargets.All)]
	public class PacketIndexAttribute : Attribute
	{
		public PacketIndexAttribute(int index, bool isReturnPacket = false, bool serializeToEnd = false,
			bool removeSeparator = false, string specialSeparator = ".")
		{
			Index = index;
			IsReturnPacket = isReturnPacket;
			SerializeToEnd = serializeToEnd;
			RemoveSeparator = removeSeparator;
			SpecialSeparator = specialSeparator;
		}

		public int Index { get; set; }

		public bool IsReturnPacket { get; set; }

		public bool RemoveSeparator { get; set; }

		public bool SerializeToEnd { get; set; }

		public bool IsOptional { get; set; }

		public string SpecialSeparator { get; set; }
	}
}