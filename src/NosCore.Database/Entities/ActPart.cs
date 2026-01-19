//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Database.Entities.Base;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NosCore.Database.Entities
{
    public class ActPart : IStaticEntity
    {
        public ActPart()
        {
            CharacterActParts = new HashSet<CharacterActPart>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public byte ActPartId { get; set; }

        public byte ActPartNumber { get; set; }

        public byte ActId { get; set; }
        public virtual Act Act { get; set; } = null!;

        public byte MaxTs { get; set; }

        public virtual HashSet<CharacterActPart> CharacterActParts { get; set; }
    }
}
