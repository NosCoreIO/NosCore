namespace NosCore.Configuration
{
	public class WebApiConfiguration : GameServerConfiguration
	{
		public string Password { get; set; }
		public ServerConfiguration WebApi { get; set; }
	}
}