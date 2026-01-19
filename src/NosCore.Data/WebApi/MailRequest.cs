//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Data.WebApi
{
    public class MailRequest
    {
        [Required]
        public MailDto? Mail { get; set; }
        public short? VNum { get; set; }
        public short? Amount { get; set; }
        public sbyte? Rare { get; set; }
        public byte? Upgrade { get; set; }
    }
}
