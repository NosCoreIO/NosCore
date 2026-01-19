//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;

namespace NosCore.Data.WebApi
{
    public class BazaarRequest
    {
        public Guid ItemInstanceId { get; set; }
        public long CharacterId { get; set; }
        public string? CharacterName { get; set; }
        public bool HasMedal { get; set; }
        public long Price { get; set; }
        public bool IsPackage { get; set; }
        public short Duration { get; set; }
        public short Amount { get; set; }
    }
}
