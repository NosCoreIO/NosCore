using NosCore.Core.Handling;
using NosCore.Core.Networking;
using NosCore.GameObject.Networking;

namespace NosCore.GameObject
{
	public class PacketController : IPacketController
	{
		protected ClientSession Session { get; set; }

		public void RegisterSession(NetworkClient clientSession)
		{
			Session = (ClientSession) clientSession;
		}
	}
}