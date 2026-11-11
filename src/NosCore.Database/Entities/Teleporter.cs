//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Interaction;
using NosCore.Database.Entities.Base;
using NosCore.Packets.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    public class Teleporter : IStaticEntity
    {
        public short Index { get; set; }

        public TeleporterType Type { get; set; }

        public virtual Map Map { get; set; }

        public short MapId { get; set; }

        public virtual MapNpc MapNpc { get; set; }

        public int MapNpcId { get; set; }

        public short MapX { get; set; }

        public short MapY { get; set; }

        // The n_run runner of the NPC menu offering this destination.
        public NrunRunnerType? Runner { get; set; }

        [Key]
        public short TeleporterId { get; set; }
    }
}
