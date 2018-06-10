using NosCore.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace NosCore.Data
{
    public class I18N_BCardDTO : IDTO
    {
        [Key]
        public int I18N_BCardId { get; set; }
        public string Key { get; set; }
        public RegionType RegionType { get; set; }
        public string Text { get; set; }
    }
}
