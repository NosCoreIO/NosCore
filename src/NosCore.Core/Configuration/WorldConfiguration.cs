//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations;
using NosCore.Shared.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Core.Configuration
{
    public class WorldConfiguration : ServerConfiguration
    {
        [Required]
        public required WebApiConfiguration MasterCommunication { get; set; }

        [Required]
        public required SqlConnectionConfiguration Database { get; set; }

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

        public bool AllClassAvailableOnCreate { get; set; }

        // How long one instance script trigger, or one of its effects, may take before the run is
        // failed and its players sent back.
        public TimeSpan InstanceScriptTimeout { get; set; } = TimeSpan.FromSeconds(30);

#pragma warning disable CA2227 // Collection properties should be read only
        public Dictionary<string, Dictionary<StarterOrigin, List<BasicEquipment>>> BasicEquipments { get; set; } = new();

        public Dictionary<string, Dictionary<StarterOrigin, List<short>>> BasicSkills { get; set; } = new();

        public List<ScheduledEventConfiguration> Events { get; set; } = new();

#pragma warning restore CA2227 // Collection properties should be read only
    }

    public enum StarterOrigin : byte
    {
        CreateAndUpgrade,
        Create56,
        Create80
    }

    public class BasicEquipment
    {
        public short VNum { get; set; }

        public short Amount { get; set; }

        public NoscorePocketType NoscorePocketType { get; set; }

        public sbyte Rare { get; set; }

        public byte Upgrade { get; set; }
    }
}
