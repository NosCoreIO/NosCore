//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Database.Entities.Base;
using NosCore.Packets.Enumerations;
using System;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    public class Miniland : IEntity
    {
        [MaxLength(255)]
        public string? MinilandMessage { get; set; } = ((short)Game18NConstString.Welcome).ToString();

        public long MinilandPoint { get; set; }

        public virtual Character Owner { get; set; } = null!;

        [Key]
        public Guid MinilandId { get; set; }

        public MinilandState State { get; set; }

        public long OwnerId { get; set; }

        public int DailyVisitCount { get; set; }

        public int VisitCount { get; set; }

        public short WelcomeMusicInfo { get; set; } = 3800;
    }
}
