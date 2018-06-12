using System.Collections.Generic;

namespace NosCore.Core.Networking
{
	public sealed class MasterClientListSingleton
	{
		private static MasterClientListSingleton instance;

		private MasterClientListSingleton()
		{
		}

		public static MasterClientListSingleton Instance => instance ?? (instance = new MasterClientListSingleton());

		public List<WorldServerInfo> WorldServers { get; set; }
	}
}