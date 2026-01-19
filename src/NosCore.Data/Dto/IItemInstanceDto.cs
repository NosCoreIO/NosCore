//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using System;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Data.Dto
{
    public interface IItemInstanceDto : IDto
    {
        [Key]
        Guid Id { get; set; }

        short Amount { get; set; }

        long? BoundCharacterId { get; set; }

        short Design { get; set; }

        int DurabilityPoint { get; set; }

        Instant? ItemDeleteTime { get; set; }

        short ItemVNum { get; set; }

        short Rare { get; set; }

        byte Upgrade { get; set; }
    }
}
