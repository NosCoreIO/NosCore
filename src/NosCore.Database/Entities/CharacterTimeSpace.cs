//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Database.Entities.Base;
using System;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    public class CharacterTimeSpace : IEntity
    {
        public virtual Character Character { get; set; }

        public long CharacterId { get; set; }

        [Key]
        public Guid CharacterTimeSpaceId { get; set; }

        public short TimeSpaceId { get; set; }

        public short FailedAttempts { get; set; }
    }
}
