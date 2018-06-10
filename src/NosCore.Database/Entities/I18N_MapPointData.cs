using NosCore.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.Database.Entities
{
    public class I18N_MapPointData
    {
        public int I18N_MapPointDataId { get; set; }
        public string Key { get; set; }
        public RegionType RegionType { get; set; }
        public string Text { get; set; }
    }
}
