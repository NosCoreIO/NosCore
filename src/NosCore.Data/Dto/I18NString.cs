//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Shared.Enumerations;
using System.Collections.Generic;

namespace NosCore.Data.Dto
{
    public class I18NString : Dictionary<RegionType, string>
    {
        public I18NString()
        {
            Add(RegionType.EN, "NONAME");
            Add(RegionType.DE, "NONAME");
            Add(RegionType.FR, "NONAME");
            Add(RegionType.IT, "NONAME");
            Add(RegionType.PL, "NONAME");
            Add(RegionType.ES, "NONAME");
            Add(RegionType.CS, "NONAME");
            Add(RegionType.TR, "NONAME");
            Add(RegionType.RU, "NONAME");
        }
    }
}
