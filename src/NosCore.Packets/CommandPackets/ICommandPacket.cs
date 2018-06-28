using JetBrains.Annotations;

namespace NosCore.Packets.CommandPackets
{
	public interface ICommandPacket
	{
		[UsedImplicitly]
        string Help();
	}
}