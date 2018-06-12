using NosCore.Shared.Enumerations;

namespace NosCore.Configuration
{
	public class LoginConfiguration : GameServerConfiguration
	{
		public SqlConnectionConfiguration Database { get; set; }

		public RegionType UserLanguage { get; set; }
        public string ClientData { get; set; }
    }
}