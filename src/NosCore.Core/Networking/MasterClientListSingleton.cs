using System.Collections.Generic;

namespace NosCore.Core.Networking
{
	public sealed class MasterClientListSingleton
	{
		private static MasterClientListSingleton _instance;

		private MasterClientListSingleton()
		{
		}

		public static MasterClientListSingleton Instance => _instance ?? (_instance = new MasterClientListSingleton());

		public List<WorldServerInfo> WorldServers { get; set; }
        public int ChannelId { get; internal set; }
    }
}