using System.Collections.Generic;

namespace NosCore.Configuration
{
    public class WorldConfiguration : WebApiConfiguration
    {
        public SqlConnectionConfiguration Database { get; set; }

        public short ConnectedAccountLimit { get; set; }

        public byte ServerGroup { get; set; }

        public bool WorldInformation { get; set; }
        public bool SceneOnCreate { get; set; }
        public string ServerName { get; set; }
        public Dictionary<FeatureFlag, bool> FeatureFlags { get; set; } = new Dictionary<FeatureFlag, bool>();
        public short MaxItemAmount { get; set; }
        public byte BackpackSize { get; set; }
        public long MaxGoldAmount { get; set; }
    }
}