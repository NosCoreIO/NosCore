
namespace NosCore.Configuration
{
    public class WorldConfiguration : WebApiConfiguration
    {
        public SqlConnectionConfiguration Database { get; set; }

        public short ConnectedAccountLimit { get; set; }

        public byte ServerGroup { get; set; }

        public bool WorldInformation { get; set; }
        public bool SceneOnCreate { get; set; }
    }
}
