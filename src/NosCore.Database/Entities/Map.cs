//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.DataAttributes;
using NosCore.Data.Enumerations.I18N;
using NosCore.Database.Entities.Base;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NosCore.Database.Entities
{
    [StaticMetaData(LoadedMessage = LogLanguageKey.MAPS_LOADED, EmptyMessage = LogLanguageKey.NO_MAP)]
    public class Map : IStaticEntity
    {
        public Map()
        {
            Character = new HashSet<Character>();
            MapMonster = new HashSet<MapMonster>();
            MapNpc = new HashSet<MapNpc>();
            Portal = new HashSet<Portal>();
            Portal1 = new HashSet<Portal>();
            ScriptedInstance = new HashSet<ScriptedInstance>();
            Teleporter = new HashSet<Teleporter>();
            MapTypeMap = new HashSet<MapTypeMap>();
            Respawn = new HashSet<Respawn>();
            RespawnMapType = new HashSet<RespawnMapType>();
        }

        public virtual ICollection<Character> Character { get; set; }

        public byte[] Data { get; set; } = null!;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public short MapId { get; set; }

        public virtual ICollection<MapMonster> MapMonster { get; set; }

        public virtual ICollection<MapNpc> MapNpc { get; set; }

        public virtual ICollection<MapTypeMap> MapTypeMap { get; set; }

        public int Music { get; set; }

        [Required]
        [MaxLength(255)]
        [I18NString(typeof(I18NMapIdData))]
        public required string Name { get; set; }

        public virtual ICollection<Portal> Portal { get; set; }

        public virtual ICollection<Portal> Portal1 { get; set; }

        public virtual ICollection<Respawn> Respawn { get; set; }

        public virtual ICollection<RespawnMapType> RespawnMapType { get; set; }

        public virtual ICollection<ScriptedInstance> ScriptedInstance { get; set; }

        public bool ShopAllowed { get; set; }

        public virtual ICollection<Teleporter> Teleporter { get; set; }
    }
}
