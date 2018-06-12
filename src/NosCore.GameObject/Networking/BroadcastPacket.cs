using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations.Interaction;

namespace NosCore.GameObject.Networking
{
	public class BroadcastPacket
	{
		#region Instantiation

		public BroadcastPacket(ClientSession session, PacketDefinition packet, ReceiverType receiver,
			string someonesCharacterName = "", long someonesCharacterId = -1, int xCoordinate = 0, int yCoordinate = 0)
		{
			Sender = session;
			Packet = packet;
			Receiver = receiver;
			SomeonesCharacterName = someonesCharacterName;
			SomeonesCharacterId = someonesCharacterId;
			XCoordinate = xCoordinate;
			YCoordinate = yCoordinate;
		}

		#endregion

		#region Properties

		public PacketDefinition Packet { get; set; }

		public ReceiverType Receiver { get; set; }

		public ClientSession Sender { get; set; }

		public long SomeonesCharacterId { get; set; }

		public string SomeonesCharacterName { get; set; }

		public int XCoordinate { get; set; }

		public int YCoordinate { get; set; }

		#endregion
	}
}