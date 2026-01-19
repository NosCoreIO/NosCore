//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations;
using NosCore.Shared.Configuration;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Core.Configuration
{
    public class WorldConfiguration : ServerConfiguration
    {
        [Required]
        public WebApiConfiguration MasterCommunication { get; set; } = null!;

        [Required]
        public SqlConnectionConfiguration Database { get; set; } = null!;

        [Range(1, short.MaxValue)]
        public short ConnectedAccountLimit { get; set; }

        public byte ServerId { get; set; }

        public bool WorldInformation { get; set; }

        public bool SceneOnCreate { get; set; }

        public string? DisplayHost { get; set; }

        public int? DisplayPort { get; set; }

        public bool StartInMaintenance { get; set; }

        [Required]
        public string? ServerName { get; set; }

#pragma warning disable CA2227 // Collection properties should be read only
        public FeatureFlags FeatureFlags { get; set; } = new();
#pragma warning restore CA2227 // Collection properties should be read only

        public short MaxItemAmount { get; set; }

        public byte BackpackSize { get; set; }

        public long MaxGoldAmount { get; set; }

        public long MaxBankGoldAmount { get; set; }

        public int MaxSpPoints { get; set; }

        public int MaxAdditionalSpPoints { get; set; }

#pragma warning disable CA2227 // Collection properties should be read only
        public Dictionary<string, List<BasicEquipment>> BasicEquipments { get; set; } = new();

        public Dictionary<string, List<short>> BasicSkills { get; set; } = new();

#pragma warning restore CA2227 // Collection properties should be read only
    }

    public class BasicEquipment
    {
        public short VNum { get; set; }

        public short Amount { get; set; }

        public NoscorePocketType NoscorePocketType { get; set; }
    }
}
