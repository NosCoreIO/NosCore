using System.ComponentModel.DataAnnotations;
using NosCore.Shared.Enumerations;

namespace NosCore.Data.I18N
{
    public class I18N_CardDTO : IDTO
    {
        [Key]
        public int I18N_CardId { get; set; }

        public string Key { get; set; }
        public RegionType RegionType { get; set; }
        public string Text { get; set; }
    }
}