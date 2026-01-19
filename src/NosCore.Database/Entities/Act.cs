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
    public class Act : IStaticEntity
    {
        public Act()
        {
            ActParts = new HashSet<ActPart>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public byte ActId { get; set; }

        [Required]
        [MaxLength(255)]
        [I18NString(typeof(I18NQuest))]
        public required string Title { get; set; }

        public virtual HashSet<ActPart> ActParts { get; set; }

        public byte Scene { get; set; }
    }
}
