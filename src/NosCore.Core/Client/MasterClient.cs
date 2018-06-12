using NosCore.Configuration;
using NosCore.Shared.Enumerations;

namespace NosCore.Core.Client
{
	public class MasterClient
	{
		public ServerType Type { get; set; }
		public string Name { get; set; }
		public ServerConfiguration WebApi { get; set; }
	}
}