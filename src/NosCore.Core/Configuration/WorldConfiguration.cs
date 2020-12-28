//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

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
        public ServerConfiguration WebApi { get; set; } = null!;

        [Required]
        public SqlConnectionConfiguration Database { get; set; } = null!;

        [Range(1, short.MaxValue)]
        public short ConnectedAccountLimit { get; set; }

        public byte ServerGroup { get; set; }

        public bool WorldInformation { get; set; }

        public bool SceneOnCreate { get; set; }

        public string? DisplayHost { get; set; }

        public int? DisplayPort { get; set; }

        public bool StartInMaintenance { get; set; }

        [Required]
        public string? ServerName { get; set; }

#pragma warning disable CA2227 // Collection properties should be read only
        public FeatureFlags FeatureFlags { get; set; } = new FeatureFlags();
#pragma warning restore CA2227 // Collection properties should be read only

        public short MaxItemAmount { get; set; }

        public byte BackpackSize { get; set; }

        public long MaxGoldAmount { get; set; }

        public long MaxBankGoldAmount { get; set; }

        public int MaxSpPoints { get; set; }

        public int MaxAdditionalSpPoints { get; set; }

#pragma warning disable CA2227 // Collection properties should be read only
        public Dictionary<string, List<BasicEquipment>> BasicEquipments { get; set; } = new Dictionary<string, List<BasicEquipment>>();
#pragma warning restore CA2227 // Collection properties should be read only
    }

    public class BasicEquipment
    {
        public short VNum { get; set; }

        public short Amount { get; set; }

        public NoscorePocketType NoscorePocketType { get; set; }
    }
}