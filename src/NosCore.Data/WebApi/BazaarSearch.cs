//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using System.Collections.Generic;

namespace NosCore.Data.WebApi
{
    public class BazaarSearch
    {
        public BazaarListType TypeFilter { get; set; }
        public byte SubTypeFilter { get; set; }
        public byte LevelFilter { get; set; }
        public byte RareFilter { get; set; }
        public byte UpgradeFilter { get; set; }
        public byte OrderFilter { get; set; }
        public List<short> ItemVNumFilter { get; set; } = new();
        public RegionType Language { get; set; }
        public int Index { get; set; }
        public int PageSize { get; set; }
    }
}
